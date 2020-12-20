/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcA2A.Communication;
using IrcA2A.DataContext;

namespace IrcA2A.GameEngine
{
    public enum GameState { AwaitingPlayers, BetweenRounds, AwaitingSubmissions, AwaitingJudgement }

    public class ActiveGame
    {
        private const int ExtraAwaitingPlayersTime = 10;
        private const int TimeBetweenRounds = 15;
        private const int TimeToAwaitJudgement = 50;
        private const int TimeToAwaitPlayers = 120;
        private const int TimeToAwaitSubmissions = 80;
        private const int WarningTime = 10;

        public readonly CommunicationService _communicationService;
        public readonly ContextService _contextService;

        internal ActiveGame(CommunicationService communicationService, ContextService contextService)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));

            using (var a2aContext = _contextService.Open())
            {
                DiscardedAdjectives.AddRange(a2aContext.AdjectiveCards
                    .Select(ac => ac.AdjectiveCardId)
                    .Distinct());
                if (DiscardedAdjectives.Count == 0)
                {
                    _communicationService.SendMessage("Unable to start A2A game: Too few Adjective cards loaded into database.");
                    throw new InvalidOperationException("Too few Adjective cards available.");
                }
                DiscardedNouns.AddRange(a2aContext.NounCards
                    .Select(nc => nc.NounCardId)
                    .Distinct());
                if (DiscardedNouns.Count < 21)
                {
                    _communicationService.SendMessage("Unalbe to start A2A game: Too few Noun cards loaded into database.");
                    throw new InvalidOperationException("Too few Adjective cards available.");
                }
            }
        }

        public Queue<string> AdjectiveCardDeck { get; } = new Queue<string>();
        public int AvailableNouns => DiscardedNouns.Count + NounCardDeck.Count;
        public List<string> AwaitingPlayers { get; } = new List<string>();
        public string CurrentAdjective { get; private set; }
        public string CurrentJudge { get; private set; }
        public List<string> DiscardedAdjectives { get; } = new List<string>();
        public List<string> DiscardedNouns { get; } = new List<string>();
        public DateTime Expiration { get; private set; } = DateTime.Now.AddSeconds(TimeToAwaitPlayers);
        public bool ExpirationWarned { get; private set; }
        public GameState GameState { get; private set; }
        public Queue<string> NounCardDeck { get; } = new Queue<string>();
        public Dictionary<string, List<string>> PlayerHands { get; } = new Dictionary<string, List<string>>();
        public LinkedList<string> PlayerOrder { get; } = new LinkedList<string>();
        public uint RoundsPlayed { get; private set; }
        public List<string> ShuffledNouns { get; } = new List<string>();
        public Dictionary<string, string> SubmittedNouns { get; } = new Dictionary<string, string>();
        public Dictionary<string, int> Wins { get; } = new Dictionary<string, int>();

        private void AddPlayer(string nick)
        {
            if (PlayerOrder.Contains(nick))
                _communicationService.SendNotice(nick, "You are aleady in the game.");
            else if (AvailableNouns >= 7)
            {
                PlayerOrder.AddLast(nick);
                Wins[nick] = 0;
                if (!PlayerHands.ContainsKey(nick))
                    PlayerHands[nick] = new List<string>(DrawNouns(7));
                var stringBuilder = new StringBuilder($"{nick.AsName()} has joined the game.");
                if (GameState == GameState.AwaitingPlayers && PlayerOrder.Count == 2)
                {
                    stringBuilder.Append(" One more player needed.");
                    Expiration = Expiration.AddSeconds(ExtraAwaitingPlayersTime);
                }
                else if (GameState == GameState.AwaitingPlayers)
                {
                    stringBuilder.Append(" Starting the game soon!");
                    Expiration = DateTime.Now.AddSeconds(WarningTime);
                    ExpirationWarned = false;
                    GameState = GameState.BetweenRounds;
                }
                _communicationService.SendMessage(stringBuilder.ToString());
            }
            else
                _communicationService.SendMessage($"Sorry {nick.AsName()}, there are too few Noun cards available for more players.");
        }

        internal void ChangeName(string nick, string newNick)
        {
            if (!PlayerHands.ContainsKey(nick))
                return;
            Wins[newNick] = Wins[nick];
            Wins.Remove(nick);
            PlayerHands[newNick] = PlayerHands[nick];
            PlayerHands.Remove(nick);
            var node = PlayerOrder.Find(nick);
            if (node != null)
                node.Value = newNick;
            var currentSubmission = SubmittedNouns.FirstOrDefault(kv => kv.Value == nick).Key;
            if (currentSubmission != null)
                SubmittedNouns[currentSubmission] = newNick;
        }

        internal ActiveGame Expire()
        {
            if (GameState == GameState.AwaitingPlayers)
            {
                _communicationService.SendMessage($"Too few players, cancelling the game... {Summarize()}");
                return null;
            }
            if (GameState == GameState.BetweenRounds)
            {
                if (AdjectiveCardDeck.Count == 0)
                {
                    AdjectiveCardDeck.ShuffeFrom(DiscardedAdjectives);
                    DiscardedAdjectives.Clear();
                }
                CurrentAdjective = AdjectiveCardDeck.Dequeue();
                CurrentJudge = PlayerOrder.First();
                PlayerOrder.RemoveFirst();
                AwaitingPlayers.AddRange(PlayerOrder);
                PlayerOrder.AddLast(CurrentJudge);
                _communicationService.SendMessage($"Round {RoundsPlayed + 1}: The judge is {CurrentJudge.AsName()}, submit your best Noun for the Adjective {CurrentAdjective.AsAdjective()}");
                foreach (var player in AwaitingPlayers)
                {
                    var deck = PlayerHands[player];
                    deck.AddRange(DrawNouns(7 - deck.Count));
                    _communicationService.SendNotice(player, $"Choose your noun for {CurrentAdjective.AsAdjective()} (say the #, not the word): {deck.AsJoinedNouns()}");
                }
                Expiration = DateTime.Now.AddSeconds(TimeToAwaitSubmissions);
                ExpirationWarned = false;
                GameState = GameState.AwaitingSubmissions;
                return this;
            }
            if (GameState == GameState.AwaitingSubmissions && !ExpirationWarned)
            {
                Expiration = DateTime.Now.AddSeconds(WarningTime);
                ExpirationWarned = true;
                _communicationService.SendMessage($"10 seconds left to submit! Still waiting on Nouns from: {string.Join(", ", AwaitingPlayers.Select(p => p.AsName()))}");
                return this;
            }
            if (GameState == GameState.AwaitingSubmissions && SubmittedNouns.Count >= 2)
            {
                foreach (var player in AwaitingPlayers)
                    PlayerOrder.Remove(player);
                ShuffledNouns.AddRange(SubmittedNouns.Keys.ToShuffled());
                _communicationService.SendMessage($"{ShuffledNouns.Count} Nouns received.{(AwaitingPlayers.Count > 0 ? " (Inactive players removed)" : "")} {CurrentJudge.AsName()}, pick the best Noun for {CurrentAdjective.AsAdjective()}: {ShuffledNouns.AsJoinedNouns()}");
                Expiration = DateTime.Now.AddSeconds(TimeToAwaitJudgement);
                ExpirationWarned = false;
                GameState = GameState.AwaitingJudgement;
                return this;
            }
            if (GameState == GameState.AwaitingSubmissions)
            {
                foreach (var player in AwaitingPlayers)
                    PlayerOrder.Remove(player);
                if (PlayerOrder.Count < 3)
                {
                    _communicationService.SendMessage($"Unfortunately, there were too few submissions. Removed inactive players, but too few players to continue. Waiting on {(PlayerOrder.Count == 1 ? "one more player" : "two more players")} to restart...");
                    ClearCurrentRound();
                    Expiration = DateTime.Now.AddSeconds(TimeToAwaitPlayers);
                    ExpirationWarned = false;
                    GameState = GameState.AwaitingPlayers;
                    return this;
                }
                _communicationService.SendMessage($"Unfortunately, there were too few submissions. Removed inactive players and starting a new round shortly...");
                ClearCurrentRound();
                Expiration = DateTime.Now.AddSeconds(TimeBetweenRounds);
                ExpirationWarned = false;
                GameState = GameState.BetweenRounds;
                return this;
            }
            if (GameState == GameState.AwaitingJudgement && !ExpirationWarned)
            {
                Expiration = DateTime.Now.AddSeconds(WarningTime);
                ExpirationWarned = true;
                _communicationService.SendMessage($"{CurrentJudge.AsName()}, we're still waiting for you to pick a winner! 10 seconds remaining...");
                return this;
            }
            if (GameState == GameState.AwaitingJudgement)
            {
                _communicationService.SendMessage($"{CurrentJudge.AsName()} took too long to decide a winner. Starting a new round shortly...");
                ClearCurrentRound();
                Expiration = DateTime.Now.AddSeconds(TimeBetweenRounds);
                ExpirationWarned = false;
                GameState = GameState.BetweenRounds;
                return this;
            }
            return this;
        }

        internal ActiveGame Input(string nick, string message)
        {
            if (message == ".join")
            {
                AddPlayer(nick);
                return this;
            }
            if (!PlayerOrder.Contains(nick))
                return this;
            if (message == ".sa2a")
            {
                _communicationService.SendMessage($"{nick.AsName()} has ended the A2A game. {Summarize()}");
                return null;
            }
            if (message == ".stats")
            {
                _communicationService.SendMessage(Summarize());
                return this;
            }
            if (message == ".leave")
                return RemovePlayer(nick);
            if (GameState == GameState.AwaitingSubmissions && AwaitingPlayers.Contains(nick) && int.TryParse(message, out var submission) && submission > 0 && submission < 8)
            {
                SubmittedNouns[PlayerHands[nick][submission - 1]] = nick;
                PlayerHands[nick].RemoveAt(submission - 1);
                AwaitingPlayers.Remove(nick);
                if (AwaitingPlayers.Count == 0)
                {
                    ShuffledNouns.AddRange(SubmittedNouns.Keys.ToShuffled());
                    _communicationService.SendMessage($"All Nouns received. {CurrentJudge.AsName()}, pick the winner for {CurrentAdjective.AsAdjective()}: {ShuffledNouns.AsJoinedNouns()}");
                    Expiration = DateTime.Now.AddSeconds(TimeToAwaitJudgement);
                    ExpirationWarned = false;
                    GameState = GameState.AwaitingJudgement;
                }
                else
                    _communicationService.SendNotice(nick, "Submission received!");
                return this;
            }
            if (GameState == GameState.AwaitingJudgement && nick == CurrentJudge && int.TryParse(message, out var pick) && pick > 0 && pick <= SubmittedNouns.Count)
            {
                var chosenNoun = ShuffledNouns[pick - 1];
                var winner = SubmittedNouns[chosenNoun];
                Wins[winner]++;
                _communicationService.SendMessage($"{winner.AsName()} is the winner of round {++RoundsPlayed} with {CurrentAdjective.AsAdjective()} {chosenNoun.AsNoun()}!");
                ClearCurrentRound();
                Expiration = DateTime.Now.AddSeconds(TimeBetweenRounds);
                ExpirationWarned = false;
                GameState = GameState.BetweenRounds;
            }
            return this;
        }

        internal ActiveGame RemovePlayer(string nick)
        {
            var node = PlayerOrder.Find(nick);
            if (node != null)
            {
                PlayerOrder.Remove(node);
                if (GameState == GameState.AwaitingPlayers && PlayerOrder.Count == 0)
                {
                    _communicationService.SendMessage($"{nick.AsName()} has left the game. Cancelling the game... {Summarize()}");
                    return null;
                }
                if (GameState == GameState.AwaitingPlayers)
                {
                    _communicationService.SendMessage($"{nick.AsName()} has left the game. Waiting on 2 more players to start...");
                    return this;
                }
                if (GameState == GameState.BetweenRounds && PlayerOrder.Count < 3)
                {
                    _communicationService.SendMessage($"{nick.AsName()} has left the game. Too few players to continue. Waiting on 1 more player to restart...");
                    Expiration = DateTime.Now.AddSeconds(TimeToAwaitPlayers);
                    ExpirationWarned = false;
                    GameState = GameState.AwaitingPlayers;
                    return this;
                }
                if (nick == CurrentJudge)
                {
                    ClearCurrentRound();
                    if (PlayerOrder.Count > 2)
                    {
                        _communicationService.SendMessage($"{nick.AsName()}, the current judge, has left the game. Starting a new round shortly...");
                        Expiration = DateTime.Now.AddSeconds(TimeBetweenRounds);
                        ExpirationWarned = false;
                        GameState = GameState.BetweenRounds;
                        return this;
                    }
                    _communicationService.SendMessage($"{nick.AsName()}, the current judge, has left the game. Too few players to continue. Waiting on 1 more player to restart...");
                    Expiration = DateTime.Now.AddSeconds(TimeToAwaitPlayers);
                    ExpirationWarned = false;
                    GameState = GameState.AwaitingPlayers;
                    return this;
                }
                if (GameState == GameState.AwaitingSubmissions && AwaitingPlayers.Contains(nick))
                {
                    if (AwaitingPlayers.Contains(nick))
                        AwaitingPlayers.Remove(nick);
                    if (AwaitingPlayers.Count + SubmittedNouns.Count < 2)
                    {
                        _communicationService.SendMessage($"{nick.AsName()} has left the game. Too few players to continue. Waiting on 1 more player to restart...");
                        ClearCurrentRound();
                        Expiration = DateTime.Now.AddSeconds(TimeToAwaitPlayers);
                        ExpirationWarned = false;
                        GameState = GameState.AwaitingPlayers;
                        return this;
                    }
                }
                _communicationService.SendMessage($"{nick.AsName()} has left the game.");
            }
            return this;
        }

        internal void StartGame(string nick)
        {
            PlayerOrder.AddLast(nick);
            Wins[nick] = 0;
            PlayerHands[nick] = new List<string>(DrawNouns(7));
            _communicationService.SendMessage($"{nick.AsName()} has started a game of A2A! Commands: '.join', '.leave', '.stats', and '.sa2a' (end game). Waiting on 2 more players...");
        }

        public string Summarize()
        {
            var top = Wins
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Value)
                .Take(5)
                .Select((kv, i) => $"{i + 1}. {kv.Key.AsName()} ({kv.Value})")
                .ToList();
            return $"Rounds played: {RoundsPlayed}, Top Winners: {(top.Count == 0 ? "None" : string.Join(", ", top))}";
        }

        private void ClearCurrentRound()
        {
            CurrentAdjective = null;
            CurrentJudge = null;
            DiscardedNouns.AddRange(ShuffledNouns);
            ShuffledNouns.Clear();
            SubmittedNouns.Clear();
        }

        private string DrawNoun()
        {
            if (NounCardDeck.Count == 0)
            {
                NounCardDeck.ShuffeFrom(DiscardedNouns);
                DiscardedNouns.Clear();
            }
            return NounCardDeck.Dequeue();
        }

        private IEnumerable<string> DrawNouns(int count)
        {
            if (NounCardDeck.Count + DiscardedNouns.Count < count)
                throw new InvalidOperationException("Too few nouns remaining in deck.");
            for (var i = 0; i < count; i++)
                yield return DrawNoun();
        }
    }
}
