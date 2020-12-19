/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.Concurrent;
using System.Threading;
using Meebey.SmartIrc4net;

namespace IrcA2A.Communication
{
    public class MessageReceiver : IDisposable
    {
        private readonly IrcClient _ircClient;
        private readonly BlockingCollection<Received> _receivedMessages = new BlockingCollection<Received>();
        private Received _failedMessage;

        public MessageReceiver(IrcClient ircClient)
        {
            _ircClient = ircClient ?? throw new ArgumentNullException(nameof(ircClient));
            _ircClient.OnChannelMessage += IrcClientOnMessage;
            _ircClient.OnKick += IrcClientGotUserKick;
            _ircClient.OnNickChange += IrcClientOnNickChange;
            _ircClient.OnPart += IrcClientOnPart;
            _ircClient.OnQuit += IrcClientGotQuit;
        }

        public void Dispose()
        {
            _ircClient.OnChannelMessage -= IrcClientOnMessage;
            _ircClient.OnKick -= IrcClientGotUserKick;
            _ircClient.OnNickChange -= IrcClientOnNickChange;
            _ircClient.OnPart -= IrcClientOnPart;
            _ircClient.OnQuit -= IrcClientGotQuit;
        }

        public void GetMessage(CancellationToken cancellationToken, Action<Received> processor)
        {
            Received received = null;
            try
            {
                received = _failedMessage ?? _receivedMessages.Take(cancellationToken);
            }
            catch (OperationCanceledException) { }
            _failedMessage = null;
            try
            {
                processor(received);
            }
            catch (Exception)
            {
                _failedMessage = received;
                throw;
            }
        }

        private void IrcClientOnMessage(object sender, IrcEventArgs e) =>
            _receivedMessages.Add(new ReceivedMessage { Sender = e.Data.Nick, Message = e.Data.Message });

        private void IrcClientGotUserKick(object sender, KickEventArgs e) =>
            _receivedMessages.Add(new ReceivedLeft { Sender = e.Whom });

        private void IrcClientOnNickChange(object sender, NickChangeEventArgs e) =>
            _receivedMessages.Add(new ReceivedNickChange { Sender = e.OldNickname, NewNick = e.NewNickname });

        private void IrcClientOnPart(object sender, PartEventArgs e) =>
            _receivedMessages.Add(new ReceivedLeft { Sender = e.Who });

        private void IrcClientGotQuit(object sender, QuitEventArgs e) =>
            _receivedMessages.Add(new ReceivedLeft { Sender = e.Who });
    }
}
