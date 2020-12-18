/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using IrcA2A.Communication;
using IrcA2A.DataContext;
using IrcA2A.GameEngine;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class ManagementViewModel : IrcA2AViewModel, IDisposable
    {
        private readonly ContextService _contextService;
        private readonly CommunicationService _communicationService;
        private readonly GameService _gameService;

        public ManagementViewModel(IUpbeatService upbeatService, ContextService contextService, CommunicationService communicationService, GameService gameService)
            : base(upbeatService)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));

            CommunicationViewModel = new CommunicationViewModel(_upbeatService, _communicationService);
            GameViewModel = new GameViewModel(_upbeatService, _communicationService, _gameService);
            HistoryViewModel = new HistoryViewModel(_upbeatService, _contextService);
        }

        public CommunicationViewModel CommunicationViewModel { get; }
        public GameViewModel GameViewModel { get; }
        public HistoryViewModel HistoryViewModel { get; }

        public void Dispose()
        {
            CommunicationViewModel.Dispose();
            GameViewModel.Dispose();
            HistoryViewModel.Dispose();
        }

        public class Parameters
        {
            public string[] Args { get; init; }
        }
    }
}
