using Microsoft.EntityFrameworkCore;
using PremierLottoApi.Models;
using PremierLottoApi.Utilities;
using PremierLottoApi.Services.Interfaces;
using System;
using System.Threading.Tasks;
using PremierLottoApi.Data;

namespace PremierLottoApi.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly AppDbContext _context;
        private const decimal MinStakeAmount = 200.00m;

        public GameSessionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> RegisterPlayerAsync(string legalName, string playerAlias, DateTime dateOfBirth)
        {
            bool aliasExists = await _context.Players
                .AnyAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());

            if (aliasExists)
            {
                throw new InvalidOperationException($"The player alias '{playerAlias}' is already taken. Please choose another alias.");
            }

            var utcDateOfBirth = dateOfBirth.Kind == DateTimeKind.Utc
        ? dateOfBirth
        : DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc);

            var newPlayer = new Player
            {
                LegalName = legalName,
                PlayerAlias = playerAlias,
                DateOfBirth = utcDateOfBirth,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            await _context.Players.AddAsync(newPlayer);
            await _context.SaveChangesAsync();

            return newPlayer.Id.ToString();
        }

        private async Task<Wallet> GetOrCreatePlayerWalletAsync(Player player)
        {
            var wallet = await _context.Wallets
                .SingleOrDefaultAsync(w => w.PlayerId == player.Id);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    PlayerId = player.Id,
                    Balance = 5000.00m,
                    DebtOwed = 5000.00m
                };
                await _context.Wallets.AddAsync(wallet);
                await _context.SaveChangesAsync();

                await _context.LedgerEntries.AddAsync(new LedgerEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Type = "HouseBonusPayout",
                    Amount = -5000.00m,
                    Description = $"Initial ₦5,000.00 Welcome Bonus issued to player {player.PlayerAlias}",
                    WalletId = wallet.Id
                });
                await _context.SaveChangesAsync();
            }

            return wallet;
        }

        public async Task<object> CreateGamePoolAsync(string gameType, string creatorAlias, decimal stakeAmount)
        {
            var player = await _context.Players.SingleAsync(p => p.PlayerAlias.ToLower() == creatorAlias.ToLower());

            if (stakeAmount < MinStakeAmount)
            {
                throw new ArgumentException($"The minimum stake amount is ₦{MinStakeAmount:N2}.");
            }

            var wallet = await GetOrCreatePlayerWalletAsync(player);

            if (wallet.Balance < stakeAmount)
            {
                throw new InvalidOperationException("Insufficient wallet balance to create and join this pool.");
            }

            decimal basePool = stakeAmount * 0.90m;

            var rollover = await _context.JackpotRollovers
                .FirstOrDefaultAsync(r => r.GameType == gameType);

            decimal carriedIn = 0;
            if(rollover != null && rollover.CarriedAmount > 0)
            {
                carriedIn = rollover.CarriedAmount;
                rollover.CarriedAmount = 0;
            }

            var newPool = new GamePool
            {
                GameType = gameType,
                Status = "Open",
                TotalPrizePool = basePool + carriedIn,
                CreatedAt = DateTime.UtcNow,
                CreatedByAlias = player.PlayerAlias
            };

            await _context.GamePools.AddAsync(newPool);
            await _context.SaveChangesAsync();

            wallet.Balance -= stakeAmount;
            player.TotalGamesPlayed += 1;

            var participant = new GamePoolParticipant
            {
                PoolId = newPool.Id,
                PlayerId = player.Id,
                StakeAmount = stakeAmount,
                JoinedAt = DateTime.UtcNow
            };

            await _context.GamePoolParticipants.AddAsync(participant);
            await _context.SaveChangesAsync();

            return new
            {
                poolId = newPool.Id,
                message = carriedIn > 0
                    ? $"Game pool #{newPool.Id} created successfully! A jackpot rollover of ₦{carriedIn:N2} was added to this pool."
                    : $"Game pool #{newPool.Id} created successfully!",
               
            };
        }

        public async Task<object> JoinSpecificPoolAsync(int poolId, string playerAlias, decimal stakeAmount)
        {
            var player = await _context.Players.SingleAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());

            if (stakeAmount < MinStakeAmount)
            {
                throw new ArgumentException($"The minimum stake amount is ₦{MinStakeAmount:N2}.");
            }

            var pool = await _context.GamePools
                .Include(p => p.Participants)
                .FirstOrDefaultAsync(p => p.Id == poolId);

            if (pool == null)
            {
                throw new KeyNotFoundException("The specified game pool does not exist.");
            }

            if (pool.Status != "Open")
            {
                throw new InvalidOperationException("This game pool is closed or already locked.");
            }

            if (pool.Participants != null && pool.Participants.Count >= 10)
            {
                throw new InvalidOperationException("This game pool is already full (max 10 players).");
            }

            bool alreadyJoined = await _context.GamePoolParticipants
                .AnyAsync(p => p.PoolId == pool.Id && p.PlayerId == player.Id);

            if (alreadyJoined)
            {
                throw new InvalidOperationException("You have already joined this game pool.");
            }

            var wallet = await GetOrCreatePlayerWalletAsync(player);
            if (wallet.Balance < stakeAmount)
            {
                throw new InvalidOperationException("Insufficient wallet balance to join this pool.");
            }

            wallet.Balance -= stakeAmount;
            player.TotalGamesPlayed += 1;

            decimal netContribution = stakeAmount * 0.90m;
            pool.TotalPrizePool += netContribution;

            var participant = new GamePoolParticipant
            {
                PoolId = pool.Id,
                PlayerId = player.Id,
                StakeAmount = stakeAmount,
                JoinedAt = DateTime.UtcNow
            };

            await _context.GamePoolParticipants.AddAsync(participant);

            int participantCount = await _context.GamePoolParticipants.CountAsync(p => p.PoolId == pool.Id);
            if (participantCount >= 10)
            {
                pool.Status = "Locked";
                await _context.SaveChangesAsync();
                await InitializeGameSessionAsync(pool.Id);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return new
            {
                message = $"Successfully joined game pool #{pool.Id}! Total prize pool may include any applicable roll-overs.",
                poolId = pool.Id,
                poolStatus = pool.Status,
                createdByAlias = pool.CreatedByAlias,
                stakedAmount = stakeAmount,
                totalPrizePool = pool.TotalPrizePool
            };
        }

        public async Task<List<object>> GetAllPoolsWithParticipantsAsync(string? status = null)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                if (normalizedStatus != "open" && normalizedStatus != "locked")
                {
                    throw new ArgumentException("Invalid status filter. Allowed values are 'Open' or 'Locked'.");
                }
            }

            var query = _context.GamePools
                .Include(p => p.Participants)
                    .ThenInclude(part => part.Player)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            }

            var pools = await query.ToListAsync();

            var responseList = new List<object>();

            foreach (var pool in pools)
            {
                var playerDetails = pool.Participants?
                    .Select(p => new
                    {
                        playerId = p.Player.Id,
                        playerAlias = p.Player.PlayerAlias,
                        legalName = p.Player.LegalName,
                        stakeAmount = p.StakeAmount,
                        joinedAt = p.JoinedAt
                    })
                    .ToList();

                responseList.Add(new
                {
                    poolId = pool.Id,
                    gameType = pool.GameType,
                    status = pool.Status,
                    totalPrizePool = pool.TotalPrizePool,
                    participantCount = playerDetails?.Count ?? 0,
                    participants = playerDetails
                });
            }

            return responseList;
        }

        public async Task<Player?> GetPlayerByAliasAsync(string playerAlias)
        {
            return await _context.Players.FirstOrDefaultAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());
        }

        public async Task<(int PoolId, string PoolStatus)> ProcessGameStakeAsync(string playerAlias, string gameType, decimal stakeAmount)
        {
            var player = await _context.Players.SingleAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());
            
            if (stakeAmount < MinStakeAmount)
            {
                throw new ArgumentException($"The minimum stake amount is ₦{MinStakeAmount:N2}.");
            }

            var wallet = await GetOrCreatePlayerWalletAsync(player);

            if (wallet.Balance < stakeAmount)
            {
                throw new InvalidOperationException("Insufficient wallet balance to stake in this game session.");
            }

            var activePool = await _context.GamePools
                .Include(p => p.Participants)
                .FirstOrDefaultAsync(p => p.GameType == gameType && p.Status == "Open" && (p.Participants == null || p.Participants.Count < 10));

            if (activePool == null)
            {
                activePool = new GamePool
                {
                    GameType = gameType,
                    Status = "Open",
                    TotalPrizePool = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.GamePools.AddAsync(activePool);
                await _context.SaveChangesAsync();
            }

            bool alreadyJoined = await _context.GamePoolParticipants
                .AnyAsync(p => p.PoolId == activePool.Id && p.PlayerId == player.Id);

            if (alreadyJoined)
            {
                throw new InvalidOperationException("You have already staked in this active game pool.");
            }

            decimal totalHouseCut = stakeAmount * 0.10m;
            decimal netContribution = stakeAmount * 0.90m;
            decimal realizedProfit = 0;
            decimal debtRecoveryAmount = 0;

            if (wallet.DebtOwed > 0)
            {
                debtRecoveryAmount = stakeAmount * 0.05m;

                if (debtRecoveryAmount > wallet.DebtOwed)
                {
                    debtRecoveryAmount = wallet.DebtOwed;
                    wallet.DebtOwed = 0;
                }
                else
                {
                    wallet.DebtOwed -= debtRecoveryAmount;
                }

                realizedProfit = totalHouseCut - debtRecoveryAmount;
            }
            else
            {
                realizedProfit = totalHouseCut;
            }

            wallet.Balance -= stakeAmount;
            player.TotalGamesPlayed += 1;

            await _context.LedgerEntries.AddAsync(new LedgerEntry
            {
                Timestamp = DateTime.UtcNow,
                Type = "Stake",
                Amount = -stakeAmount,
                Description = $"{gameType} Pool #{activePool.Id} - Stake",
                WalletId = wallet.Id
            });

            if (realizedProfit > 0)
            {
                await _context.LedgerEntries.AddAsync(new LedgerEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Type = "HouseProfit",
                    Amount = +realizedProfit,
                    Description = $"{gameType} Pool #{activePool.Id} - House Profit",
                    WalletId = wallet.Id
                });
            }

            if (debtRecoveryAmount > 0)
            {
                await _context.LedgerEntries.AddAsync(new LedgerEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Type = "DebtRecovery",
                    Amount = +debtRecoveryAmount,
                    Description = $"{gameType} Pool #{activePool.Id} - Debt Recovery",
                    WalletId = wallet.Id
                });
            }

            var participant = new GamePoolParticipant
            {
                PoolId = activePool.Id,
                PlayerId = player.Id,
                StakeAmount = stakeAmount,
                JoinedAt = DateTime.UtcNow
            };

            await _context.GamePoolParticipants.AddAsync(participant);
            activePool.TotalPrizePool += netContribution;

            int participantCount = await _context.GamePoolParticipants
                .CountAsync(p => p.PoolId == activePool.Id);

            if (participantCount >= 10)
            {
                activePool.Status = "Locked";
                await _context.SaveChangesAsync();
                await InitializeGameSessionAsync(activePool.Id);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return (activePool.Id, activePool.Status);
        }
        public async Task<(int PoolId, string PoolStatus, decimal TotalPrizePool, string Message)> ClosePoolEarlyAsync(int poolId, string playerAlias)
        {
            var playerExists = await _context.Players
                .AnyAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());

            if (!playerExists)
            {
                throw new KeyNotFoundException("Invalid Player alias.");
            }

            var pool = await _context.GamePools.FindAsync(poolId);
            if (pool == null)
            {
                throw new KeyNotFoundException("Game pool not found.");
            }

            if (pool.Status == "Locked")
            {
                throw new InvalidOperationException("This pool is already locked.");
            }

            bool isParticipant = await _context.GamePoolParticipants
                .Include(p => p.Player)
                .AnyAsync(p => p.PoolId == poolId && p.Player.PlayerAlias.ToLower() == playerAlias.ToLower());

            if (!isParticipant)
            {
                throw new UnauthorizedAccessException("You are not a participant in this pool, so you cannot close it.");
            }

            int participantCount = await _context.GamePoolParticipants
                .CountAsync(p => p.PoolId == poolId);

            if (participantCount < 2)
            {
                throw new InvalidOperationException($"Cannot close pool early. It only has {participantCount} player. A minimum of 2 players is required.");
            }

            pool.Status = "Locked";

            await InitializeGameSessionAsync(pool.Id);

            await _context.SaveChangesAsync();

            return (pool.Id, pool.Status, pool.TotalPrizePool, $"Pool #{pool.Id} successfully closed early with {participantCount} participants!");
        }
        public async Task InitializeGameSessionAsync(int poolId)
        {
            var pool = await _context.GamePools.FindAsync(poolId);
            if (pool == null) return;

            bool sessionExists = await _context.GameSessions
                .AnyAsync(s => s.PoolId == poolId);

            if (sessionExists) return;

            int totalRounds = pool.GameType switch
            {
                "Easy" => 2,
                "Classic" => 3,
                "Pro" => 5,
                _ => 2
            };

            var session = new GameSession
            {
                PoolId = pool.Id,
                GameType = pool.GameType,
                TotalRounds = totalRounds,
                CurrentRound = 1,
                Status = "Active"
            };

            await _context.GameSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            await GenerateNewRoundAsync(session.Id, 1, pool.GameType);
        }
        public async Task<(PlayerGuess playerguess, int roundNumber, string sessionStatus, bool isTieBreaker)> SubmitRoundGuessesAsync(int poolId, string playerAlias, string rawGuesses)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());
            if (player == null) throw new KeyNotFoundException("Invalid Player alias.");

            bool isInPool = await _context.GamePoolParticipants
                .Include(p => p.Player)
                .AnyAsync(p => p.PoolId == poolId && p.Player.PlayerAlias.ToLower() == playerAlias.ToLower());

            if (!isInPool)
            {
                throw new InvalidOperationException("You are not a participant in this game pool.");
            }

            var session = await _context.GameSessions
                .FirstOrDefaultAsync(s => s.PoolId == poolId && s.Status == "Active");

            if (session == null)
            {
                throw new InvalidOperationException("No active game session found for this pool.");
            }

            var currentRound = await _context.GameRounds
                .FirstOrDefaultAsync(r => r.GameSessionId == session.Id && r.RoundNumber == session.CurrentRound);

            if (currentRound == null) throw new InvalidOperationException("Current game round not found.");

            var poolParticipants = await _context.GamePoolParticipants
                .Where(p => p.PoolId == poolId)
                .OrderBy(p => p.Id)
                .Include(p => p.Player)
                .ToListAsync();

            int guessesSubmittedThisRound = await _context.PlayerGuesses
                .CountAsync(pg => pg.GameSessionId == session.Id && pg.RoundId == currentRound.Id);

            int expectedPlayerIndex = guessesSubmittedThisRound % poolParticipants.Count;
            var expectedPlayer = poolParticipants[expectedPlayerIndex].Player;

            if (player.Id != expectedPlayer.Id)
            {
                throw new InvalidOperationException($"It is not your turn yet! Waiting for {expectedPlayer.PlayerAlias} to submit.");
            }

            var existingGuesses = await _context.PlayerGuesses
                .FirstOrDefaultAsync(pg => pg.GameSessionId == session.Id && pg.RoundId == currentRound.Id && pg.PlayerId == player.Id);

            if (existingGuesses != null)
            {
                throw new InvalidOperationException("You have already submitted your guesses for this round.");
            }

            var guessList = rawGuesses
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .ToList();

            if (guessList.Count != 4)
            {
                throw new ArgumentException("You must provide exactly 4 guesses separated by commas or spaces.");
            }

            if ((session.GameType == "Classic" || session.GameType == "Pro") && guessList.Distinct(StringComparer.OrdinalIgnoreCase).Count() != guessList.Count)
            {
                throw new ArgumentException("Classic and Pro games do not allow duplicate values in your guesses.");
            }

            GameValidator.ValidateGuessRanges(session.GameType, guessList);

            var winningValues = currentRound.WinningValues.Split(',').Select(v => v.Trim()).ToList();
            int matchesCount = 0;

            for (int i = 0; i < 4; i++)
            {
                if (winningValues.Contains(guessList[i], StringComparer.OrdinalIgnoreCase))
                {
                    matchesCount++;
                }
            }

            int requiredMatches = session.GameType switch
            {
                "Easy" => 1,
                "Classic" => 2,
                "Pro" => 3,
                _ => 1
            };

            bool metThreshold = matchesCount >= requiredMatches;

            var playerGuess = new PlayerGuess
            {
                GameSessionId = session.Id,
                RoundId = currentRound.Id,
                PlayerId = player.Id,
                Guesses = string.Join(",", guessList),
                MatchesCount = matchesCount,
                MetThreshold = metThreshold
            };

            await _context.PlayerGuesses.AddAsync(playerGuess);
            await _context.SaveChangesAsync();

            await CheckAndAdvanceRoundAsync(poolId);

            var updatedSession = await _context.GameSessions.FindAsync(session.Id);

            int standardMaxRounds = updatedSession.GameType switch { "Easy" => 2, "Classic" => 3, "Pro" => 5, _ => 2 };
            bool isTieBreaker = updatedSession.CurrentRound > standardMaxRounds && updatedSession.Status == "Active";

            return (playerGuess, updatedSession.CurrentRound, updatedSession.Status, isTieBreaker);
        }


        public async Task<List<object>> GetPoolsByPlayerAliasAsync(string playerAlias, string? status = null)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                if (normalizedStatus != "open" && normalizedStatus != "locked")
                {
                    throw new ArgumentException("Invalid status filter. Allowed values are 'Open' or 'Locked'.");
                }
            }

            var player = await _context.Players.SingleAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());

            var query = _context.GamePoolParticipants
                .Where(p => p.PlayerId == player.Id)
                .Include(p => p.Pool)
                    .ThenInclude(pool => pool.Participants)
                .Select(p => p.Pool)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(pool => pool.Status.ToLower() == status.ToLower());
            }

            var pools = await query.ToListAsync();

            var responseList = new List<object>();

            foreach (var pool in pools)
            {
                responseList.Add(new
                {
                    poolId = pool.Id,
                    gameType = pool.GameType,
                    status = pool.Status,
                    totalPrizePool = pool.TotalPrizePool,
                    participantCount = pool.Participants?.Count ?? 0,
                    createdAt = pool.CreatedAt
                });
            }

            return responseList;
        }
        private async Task GenerateNewRoundAsync(int sessionId, int roundNumber, string gameType)
        {
            var random = new Random();
            List<string> generatedValues = new List<string>();
                
            if (gameType == "Easy")
            {
                for (int i = 0; i < 4; i++)
                {
                    generatedValues.Add(random.Next(0, 31).ToString());
                }
                    
            }
            else if (gameType == "Classic")
            {
                var uniqueValues = new HashSet<string>();
                while (uniqueValues.Count < 4)
                {
                    uniqueValues.Add(random.Next(0, 61).ToString());
                }
                generatedValues = uniqueValues.ToList();
            }
            else if (gameType == "Pro")
            {
                var uniqueValues = new HashSet<string>();
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                while(uniqueValues.Count < 4)
                {
                    char randomChar = chars[random.Next(chars.Length)];
                    int randomNum = random.Next(0, 91);
                    uniqueValues.Add($"{randomChar}{randomNum}");
                }

                generatedValues = uniqueValues.ToList();
            }
            
            var round = new GameRound
            {
                GameSessionId = sessionId,
                RoundNumber = roundNumber,
                WinningValues = string.Join(",", generatedValues)
            };

            await _context.GameRounds.AddAsync(round);
            await _context.SaveChangesAsync();
        }
        public async Task CheckAndAdvanceRoundAsync(int poolId)
        {
            var session = await _context.GameSessions
                .FirstOrDefaultAsync(s => s.PoolId == poolId && s.Status == "Active");
            if (session == null) return;

            var pool = await _context.GamePools.FindAsync(poolId);
            if (pool == null) return;

            int totalPlayersInPool = await _context.GamePoolParticipants
                .CountAsync(p => p.PoolId == pool.Id);

            var currentRound = await _context.GameRounds
                .FirstOrDefaultAsync(r => r.GameSessionId == session.Id && r.RoundNumber == session.CurrentRound);

            if (currentRound == null) return;

            int guessesCountThisRound = await _context.PlayerGuesses
                .CountAsync(pg => pg.GameSessionId == session.Id && pg.RoundId == currentRound.Id);

            if (guessesCountThisRound < totalPlayersInPool)
            {
                return;
            }
            if (session.CurrentRound >= session.TotalRounds)
            {
                if (session.GameType != "Easy")
                {
                    var topScores = await _context.PlayerGuesses
                        .Where(pg => pg.GameSessionId == session.Id)
                        .GroupBy(pg => pg.PlayerId)
                        .Select(g => new { PlayerId = g.Key, TotalMatches = g.Sum(x => x.MatchesCount) })
                        .OrderByDescending(x => x.TotalMatches)
                        .ToListAsync();

                    if (topScores.Count >= 2 && topScores[0].TotalMatches == topScores[1].TotalMatches && topScores[0].TotalMatches > 0)
                    {
                        session.TotalRounds += 1;
                        session.CurrentRound += 1;
                        await _context.SaveChangesAsync();

                        await GenerateNewRoundAsync(session.Id, session.CurrentRound, session.GameType);
                        return; 
                    }
                }

                session.Status = "Completed";
                await _context.SaveChangesAsync();

                await CalculateWinnersAndDistributePayoutsAsync(pool.Id);
            }
            else
            {
                session.CurrentRound += 1;
                await _context.SaveChangesAsync();

                await GenerateNewRoundAsync(session.Id, session.CurrentRound, session.GameType);
            }
        }

        public async Task<(List<object> winners, decimal rolledOverAmount)> CalculateWinnersAndDistributePayoutsAsync(int poolId)
        {
            var session = await _context.GameSessions
                .FirstOrDefaultAsync(s => s.PoolId == poolId);
            if (session == null) return (new List<object>(), 0);

            var pool = await _context.GamePools.FindAsync(poolId);
            if (pool == null) return (new List<object>(), 0);

            var playerScores = await _context.PlayerGuesses
                .Where(pg => pg.GameSessionId == session.Id)
                .GroupBy(pg => pg.PlayerId)
                .Select(group => new
                {
                    PlayerId = group.Key,
                    TotalMatches = group.Sum(g => g.MatchesCount)
                })
                .Where(x => x.TotalMatches > 0)
                .ToListAsync();

            if (!playerScores.Any())
            {
                decimal rolloverAmount = pool.TotalPrizePool;
                var rollover = await _context.JackpotRollovers
                    .FirstOrDefaultAsync(r => r.GameType == session.GameType);

                if (rollover == null)
                {
                    rollover = new JackpotRollover
                    {
                        GameType = session.GameType,
                        CarriedAmount = rolloverAmount
                    };
                    await _context.JackpotRollovers.AddAsync(rollover);
                }
                else
                {
                    rollover.CarriedAmount += rolloverAmount;
                }

                pool.TotalPrizePool = 0;
                await _context.SaveChangesAsync();

                return (new List<object>(), rolloverAmount);
            }

            int highestScore = playerScores.Max(x => x.TotalMatches);

            var winnersList = playerScores
                .Where(x => x.TotalMatches == highestScore)
                .ToList();

            var winnerDetails = new List<object>();

            if (winnersList.Any())
            {
                decimal prizePerWinner = pool.TotalPrizePool / winnersList.Count;

                foreach (var winnerScore in winnersList)
                {
                    var player = await _context.Players.FindAsync(winnerScore.PlayerId);
                    if (player != null)
                    {
                        player.TotalWins += 1;

                        if (winnerScore.TotalMatches> player.BestScore)
                        {
                            player.BestScore = winnerScore.TotalMatches;
                        }

                        if(player.TotalGamesPlayed > 0)
                        {
                            player.AverageScore = ((player.AverageScore * (player.TotalGamesPlayed - 1)) + winnerScore.TotalMatches) / player.TotalGamesPlayed;
                        }
                        var wallet = await _context.Wallets
                            .FirstOrDefaultAsync(w => w.PlayerId == player.Id);

                        if (wallet != null)
                        {
                            wallet.Balance += prizePerWinner;

                            var ledgerEntry = new LedgerEntry
                            {
                                Timestamp = DateTime.UtcNow,
                                Type = "WinPayout",
                                Amount = +prizePerWinner,
                                Description = $"Payout for winning Game Session #{session.Id} in Pool #{pool.Id} with {winnerScore.TotalMatches} total matches",
                                WalletId = wallet.Id
                            };

                            await _context.LedgerEntries.AddAsync(ledgerEntry);

                            winnerDetails.Add(new
                            {
                                playerId = player.Id,
                                playerAlias = player.PlayerAlias,
                                totalMatches = winnerScore.TotalMatches,
                                prizeWon = prizePerWinner
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            return (winnerDetails, 0);
        }
    }
}