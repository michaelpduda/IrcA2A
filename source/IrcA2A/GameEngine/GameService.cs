/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Linq;
using System.Threading;
using IrcA2A.Communication;
using IrcA2A.DataContext;

namespace IrcA2A.GameEngine
{
    public class GameService : IDisposable
    {
        private readonly ContextService _contextService;
        private readonly CommunicationService _communicationService;
        private readonly ManualResetEvent _ended = new ManualResetEvent(false);
        private CancellationTokenSource _serviceEndingSource = new CancellationTokenSource();

        public GameService(ContextService contextService, CommunicationService communicationService)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));

            ThreadPool.QueueUserWorkItem(_ => RunEngine());
        }

        public event EventHandler<ExceptionEventArgs> ExceptionThrown;
        public event EventHandler GameStateChanged;

        public ActiveGame ActiveGame { get; private set; }

        private void RunEngine()
        {
            while (!_serviceEndingSource.IsCancellationRequested)
            {
                using (var connectedSource = new CancellationTokenSource())
                using (var continueSource = CancellationTokenSource.CreateLinkedTokenSource(_serviceEndingSource.Token, connectedSource.Token))
                {
                    var connectedHandler = new EventHandler((_, _) => connectedSource.Cancel());
                    _communicationService.Connected += connectedHandler;
                    continueSource.Token.WaitHandle.WaitOne();
                    _communicationService.Connected -= connectedHandler;
                }
                if (_serviceEndingSource.IsCancellationRequested)
                    break;
                GameStateChanged?.Invoke(this, EventArgs.Empty);
                using (var disconnectedSource = new CancellationTokenSource())
                using (var endedSource = CancellationTokenSource.CreateLinkedTokenSource(_serviceEndingSource.Token, disconnectedSource.Token))
                {
                    var disconnectedHandler = new EventHandler((_, _) => disconnectedSource.Cancel());
                    _communicationService.Disconnected += disconnectedHandler;
                    using var messageReceiver = _communicationService.Open();
                    while (!endedSource.IsCancellationRequested)
                    {
                        try
                        {
                            using (var expiredSource = ActiveGame != null
                                ? new CancellationTokenSource(ActiveGame.Expiration - DateTime.Now)
                                : new CancellationTokenSource())
                            using (var abandonedSource = CancellationTokenSource.CreateLinkedTokenSource(endedSource.Token, expiredSource.Token))
                                messageReceiver.GetMessage(abandonedSource.Token,
                                    received =>
                                    {
                                        if (received == null)
                                        {
                                            if (endedSource.IsCancellationRequested)
                                                return;
                                            ActiveGame = ActiveGame?.Expire();
                                            GameStateChanged?.Invoke(this, EventArgs.Empty);
                                        }
                                        else if (ActiveGame == null)
                                        {
                                            if (received is ReceivedMessage receivedMessage && receivedMessage.Message == ".a2a")
                                            {
                                                ActiveGame = new ActiveGame(_communicationService, _contextService);
                                                ActiveGame.StartGame(receivedMessage.Sender);
                                                GameStateChanged?.Invoke(this, EventArgs.Empty);
                                            }
                                        }
                                        else
                                        {
                                            if (received is ReceivedMessage receivedMessage)
                                            {
                                                ActiveGame = ActiveGame.Input(receivedMessage.Sender, receivedMessage.Message);
                                                GameStateChanged?.Invoke(this, EventArgs.Empty);
                                            }
                                            else if (received is ReceivedLeft receivedLeft)
                                            {
                                                ActiveGame = ActiveGame.RemovePlayer(receivedLeft.Sender);
                                                GameStateChanged?.Invoke(this, EventArgs.Empty);
                                            }
                                            else if (received is ReceivedNickChange receivedNickChange)
                                                ActiveGame.ChangeName(receivedNickChange.Sender, receivedNickChange.NewNick);
                                        }
                                    });
                        }
                        catch (Exception e)
                        {
                            _communicationService.SendMessage($"A2A game has crashed... {e.GetType().Name} {e.StackTrace?.Split(Environment.NewLine.ToCharArray()).FirstOrDefault().Replace("   ", "")}: {e.Message}");
                            ActiveGame = null;
                            ExceptionThrown?.Invoke(this, new ExceptionEventArgs { Exception = e });
                            GameStateChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    _communicationService.Disconnected -= disconnectedHandler;
                }
                GameStateChanged?.Invoke(this, EventArgs.Empty);
            }
            _ended.Set();
        }

        public void Dispose()
        {
            _serviceEndingSource.Cancel();
            _ended.WaitOne();
            _serviceEndingSource.Dispose();
        }
    }
}
