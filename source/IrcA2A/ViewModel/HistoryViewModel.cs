/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IrcA2A.DataContext;
using IrcA2A.DataModel;
using UpbeatUI.ViewModel;

namespace IrcA2A.ViewModel
{
    public class HistoryViewModel : IrcA2AViewModel, IDisposable
    {
        private readonly SynchronizableCollection<string> _adjectives = new SynchronizableCollection<string>();
        private readonly ContextService _contextService;
        private readonly SynchronizableCollection<string> _nouns = new SynchronizableCollection<string>();
        private readonly SynchronizableCollection<PlayerViewModel> _players = new SynchronizableCollection<PlayerViewModel>();
        private int _adjectivesCount;
        private int _nounsCount;
        private int _roundsPlayed;

        public HistoryViewModel(IUpbeatService upbeatService, ContextService contextService)
            : base(upbeatService)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));

            Adjectives = new ReadOnlyObservableCollection<string>(_adjectives);
            Nouns = new ReadOnlyObservableCollection<string>(_nouns);
            Players = new ReadOnlyObservableCollection<PlayerViewModel>(_players);

            AddAdjectivesCommand = new DelegateCommand(AddAdjectivesAsync, exceptionCallback: ShowError);
            AddNounsCommand = new DelegateCommand(AddNounsAsync, exceptionCallback: ShowError);

            using (var a2aContext = contextService.Open())
                Refresh(a2aContext);

            contextService.Updated += ContextUpdated;
        }

        public ICommand AddAdjectivesCommand { get; }
        public ICommand AddNounsCommand { get; }
        public INotifyCollectionChanged Adjectives { get; }
        public int AdjectivesCount { get => _adjectivesCount; private set => SetProperty(ref _adjectivesCount, value); }
        public int NounsCount { get => _nounsCount; private set => SetProperty(ref _nounsCount, value); }
        public INotifyCollectionChanged Nouns { get; }
        public int RoundsPlayed { get => _roundsPlayed; private set => SetProperty(ref _roundsPlayed, value); }
        public INotifyCollectionChanged Players { get; }

        public void Dispose() =>
            _contextService.Updated -= ContextUpdated;

        private async Task AddAdjectivesAsync()
        {
            var parameters = new InputViewModel.Parameters
            {
                CommandName = "Add",
                TypeName = "Adjective",
            };
            await _upbeatService.OpenViewModelAsync(parameters);
            if (parameters.ReturnedInput == null)
                return;
            var newAdjectives = parameters.ReturnedInput.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Distinct();
            using var a2aContext = _contextService.Open();
            var existingAdjectives = a2aContext.AdjectiveCards.Select(ac => ac.AdjectiveCardId);
            var toAdd = newAdjectives.Except(existingAdjectives);
            await a2aContext.AdjectiveCards.AddRangeAsync(toAdd.Select(a => new AdjectiveCard { AdjectiveCardId = a }));
            await a2aContext.SaveChangesAsync();
            Refresh(a2aContext);
        }

        private async Task AddNounsAsync()
        {
            var parameters = new InputViewModel.Parameters
            {
                CommandName = "Add",
                TypeName = "Noun",
            };
            await _upbeatService.OpenViewModelAsync(parameters);
            if (parameters.ReturnedInput == null)
                return;
            var newNouns = parameters.ReturnedInput.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Distinct();
            using var a2aContext = _contextService.Open();
            var existingNouns = a2aContext.NounCards.Select(ac => ac.NounCardId);
            var toAdd = newNouns.Except(existingNouns);
            await a2aContext.NounCards.AddRangeAsync(toAdd.Select(a => new NounCard { NounCardId = a }));
            await a2aContext.SaveChangesAsync();
            Refresh(a2aContext);
        }

        private void ContextUpdated(object sender, Microsoft.EntityFrameworkCore.SavedChangesEventArgs e) =>
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                using var a2aContext = _contextService.Open();
                Refresh(a2aContext);
            });

        private void Refresh(A2AContext a2aContext)
        {
            AdjectivesCount = a2aContext.AdjectiveCards.Count();
            NounsCount = a2aContext.NounCards.Count();
            RoundsPlayed = a2aContext.PlayedRounds.Count();
            _players.Synchronize(
                (p, vm) =>
                {
                    vm.GamesJudged = p.JudgedRounds.Count;
                    vm.GamesPlayed = p.PlayedRounds.Count;
                    vm.GamesWon = p.WonRounds.Count;
                    vm.Name = p.PlayerId;
                },
                a2aContext.Players);
            _adjectives.Synchronize(a2aContext.AdjectiveCards.Select(ac => ac.AdjectiveCardId));
            _nouns.Synchronize(a2aContext.NounCards.Select(nc => nc.NounCardId));
        }

        private class PlayerViewModel : BaseViewModel
        {
            private int _gamesJudged;
            private int _gamesPlayed;
            private int _gamesWon;
            private string _name;

            public int GamesJudged { get => _gamesJudged; internal set => SetProperty(ref _gamesJudged, value); }
            public int GamesPlayed { get => _gamesPlayed; internal set => SetProperty(ref _gamesPlayed, value); }
            public int GamesWon { get => _gamesWon; internal set => SetProperty(ref _gamesWon, value); }
            public string Name { get => _name; internal set => SetProperty(ref _name, value); }
        }
    }
}
