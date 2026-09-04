using Microsoft.EntityFrameworkCore;
using PremierLottoApi.Models;

namespace PremierLottoApi.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<GamePool> GamePools => Set<GamePool>();
        public DbSet<GamePoolParticipant> GamePoolParticipants => Set < GamePoolParticipant >();
        public DbSet<GameRound> GameRounds => Set<GameRound>();
        public DbSet<GameSession> GameSessions => Set<GameSession>();
        public DbSet<PlayerGuess> PlayerGuesses => Set<PlayerGuess>();
    }
}