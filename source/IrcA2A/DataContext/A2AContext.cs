/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using IrcA2A.DataModel;
using Microsoft.EntityFrameworkCore;

namespace IrcA2A.DataContext
{
    public class A2AContext : DbContext
    {
        private EventHandler<SavedChangesEventArgs> _onSavedChanges;

        internal A2AContext(EventHandler<SavedChangesEventArgs> onSavedChanges)
            : base()
        {
            Database.EnsureCreated();
            _onSavedChanges = onSavedChanges;
            SavedChanges += _onSavedChanges;
        }
        private const string ConnectionString = "Filename=IrcA2A.dat";

        public DbSet<AdjectiveCard> AdjectiveCards { get; set; }
        public DbSet<NounCard> NounCards { get; set; }
        public DbSet<PlayedRound> PlayedRounds { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerLinkage> PlayerLinkages { get; set; }

        public override void Dispose()
        {
            SavedChanges -= _onSavedChanges;
            base.Dispose();
        }

        public Player FindOrCreatePlayer(string nickname)
        {
            var player = Players.Find(nickname);
            if (player != null)
                return player;
            player = new Player { PlayerId = nickname };
            Players.Add(player);
            return player;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            base.OnConfiguring(optionsBuilder
                .UseSqlite(ConnectionString)
                .UseLazyLoadingProxies());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayedRound>()
                .HasOne(pr => pr.AdjectiveCard)
                .WithMany(ac => ac.PlayedRounds);
            modelBuilder.Entity<PlayedRound>()
                .HasOne(pr => pr.Judge)
                .WithMany(p => p.JudgedRounds);
            modelBuilder.Entity<PlayedRound>()
                .HasMany(pr => pr.Players)
                .WithMany(p => p.PlayedRounds);
            modelBuilder.Entity<PlayedRound>()
                .HasMany(pr => pr.PlayedCards)
                .WithMany(nc => nc.PlayedRounds);
            modelBuilder.Entity<PlayedRound>()
                .HasOne(pr => pr.WinningCard)
                .WithMany(nc => nc.WonRounds);
            modelBuilder.Entity<PlayedRound>()
                .HasOne(pr => pr.WinningPlayer)
                .WithMany(p => p.WonRounds);
            modelBuilder.Entity<PlayerLinkage>()
                .HasMany(pl => pl.LinkedPlayers)
                .WithOne(p => p.PlayerLinkage);
        }
    }
}
