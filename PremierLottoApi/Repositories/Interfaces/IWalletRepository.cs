using PremierLottoApi.Models;
using System.Threading.Tasks;

namespace PremierLottoApi.Repositories
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByPlayerIdAsync(int playerId);
    }
}