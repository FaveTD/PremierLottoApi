using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PremierLottoApi.Models
{
    public class JackpotRollover
    {
        [Key]
        public string GameType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CarriedAmount { get; set; } = 0;
    }
}
