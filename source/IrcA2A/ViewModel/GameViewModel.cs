/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using IrcA2A.Communication;
using IrcA2A.GameEngine;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class GameViewModel : IrcA2AViewModel, IDisposable
    {
        private readonly CommunicationService _communicationService;
        private readonly GameService _gameService;
        private readonly SynchronizableCollection<PlayerViewModel> _players = new SynchronizableCollection<PlayerViewModel>();
        private string _currentState;

        public GameViewModel(IUpbeatService upbeatService, CommunicationService communicationService, GameService gameService)
            : base(upbeatService)
        {
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));

            _gameService.GameStateChanged += GameServiceStateChanged;
            _gameService.ExceptionThrown += ShowGameError;

            Players = new ReadOnlyObservableCollection<PlayerViewModel>(_players);

            Synchronize();
        }

        public string CurrentAdjective { get; private set; }
        public string CurrentJudge { get; private set; }
        public string CurrentState { get => _currentState; private set => SetProperty(ref _currentState, value); }
        public INotifyCollectionChanged Players { get; }
        public uint RoundsPlayed { get; private set; }

        public void Dispose() =>
            _gameService.GameStateChanged -= GameServiceStateChanged;

        private void GameServiceStateChanged(object sender, EventArgs e) =>
            Application.Current.Dispatcher.Invoke(Synchronize);

        private void ShowGameError(object sender, ExceptionEventArgs e) =>
            ShowError(e.Exception);

        private void Synchronize()
        {
            CurrentState = !_communicationService.IsConnected ? "Disconnected" : _gameService.ActiveGame?.GameState.ToString() ?? "Standing By";
            RoundsPlayed = _gameService.ActiveGame?.RoundsPlayed ?? 0;
            CurrentJudge = _gameService.ActiveGame?.CurrentJudge ?? "";
            CurrentAdjective = _gameService.ActiveGame?.CurrentAdjective ?? "";
            _players.Synchronize(
                (p, vm) => vm.Synchronize(
                    p,
                    _gameService.ActiveGame.Wins[p],
                    _gameService.ActiveGame.PlayerOrder.Contains(p),
                    _gameService.ActiveGame.PlayerHands.TryGetValue(p, out var nouns) ? nouns : Enumerable.Empty<string>()),
                _gameService == null ? Enumerable.Empty<string>()
                    : _gameService.ActiveGame?.PlayerOrder
                        .Concat(_gameService.ActiveGame.PlayerHands.Keys.Except(_gameService.ActiveGame.PlayerOrder))
                        ?? Enumerable.Empty<string>());
        }

        private class PlayerViewModel : BaseViewModel
        {
            private bool _isActive;
            private string _name;
            private readonly SynchronizableCollection<string> _nouns = new SynchronizableCollection<string>();
            private int _wins;

            public PlayerViewModel() =>
                Nouns = new ReadOnlyObservableCollection<string>(_nouns);

            public bool IsActive { get => _isActive; private set => SetProperty(ref _isActive, value); }
            public string Name { get => _name; private set => SetProperty(ref _name, value); }
            public INotifyCollectionChanged Nouns { get; }
            public int Wins { get => _wins; private set => SetProperty(ref _wins, value); }

            internal void Synchronize(string name, int wins, bool isActive, IEnumerable<string> nouns)
            {
                IsActive = isActive;
                Name = name;
                _nouns.Synchronize(nouns);
                Wins = wins;
            }
        }
    }
}
