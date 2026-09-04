using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PremierLottoApi.Models
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 5000.00m; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal DebtOwed { get; set; } = 5000.00m;

        public int PlayerId { get; set; }

    }
}


