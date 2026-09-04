using PremierLottoApi.DTOs;
using PremierLottoApi.Models;
using System.Threading.Tasks;

namespace PremierLottoApi.Services.Interfaces
{
    public interface IGameSessionService
    {
        Task<string> RegisterPlayerAsync(string legalName, string playerAlias, DateTime dateOfBirth);
        Task<int> CreateGamePoolAsync(string gameType, string creatorAlias, decimal stakeAmount);
        Task<object> JoinSpecificPoolAsync(int poolId, string playerAlias, decimal stakeAmount);
        Task<Player?> GetPlayerByAliasAsync(string playerAlias);
        Task<List<object>> GetAllPoolsWithParticipantsAsync(string? status = null);
        Task<List<object>> GetPoolsByPlayerAliasAsync(string playerAlias, string? status = null);
        Task<(int PoolId, string PoolStatus)> ProcessGameStakeAsync(string playerAlias, string gameType, decimal stakeAmount);
        Task<(int PoolId, string PoolStatus,decimal TotalPrizePool, string Message)> ClosePoolEarlyAsync(int poolId, string playerAlias);
        Task InitializeGameSessionAsync(int poolId);
        Task<(PlayerGuess playerguess, int roundNumber, string sessionStatus, bool isTieBreaker)> SubmitRoundGuessesAsync(int poolId, string playerAlias, string rawGuesses);
        Task CheckAndAdvanceRoundAsync(int sessionId);
        Task<List<object>> CalculateWinnersAndDistributePayoutsAsync(int sessionId);
    }
}