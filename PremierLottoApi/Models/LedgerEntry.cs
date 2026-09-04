using PremierLottoApi.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PremierLottoApi.Models
{
    public class LedgerEntry
    {
        [Key]
        public int Id { get; set; } 

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public int WalletId { get; set; }

        public Wallet Wallet { get; set; }
    }
}