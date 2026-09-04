using Microsoft.EntityFrameworkCore;
using PremierLottoApi.Data;
using PremierLottoApi.Models;
using System.Threading.Tasks;

namespace PremierLottoApi.Repositories
{
    public class WalletRepository(AppDbContext context) : IWalletRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Wallet?> GetByPlayerIdAsync(int playerId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.PlayerId == playerId);
        }

    }
}