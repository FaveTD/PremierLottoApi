using PremierLottoApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PremierLottoApi.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        [MaxLength(100)]
        public string LegalName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PlayerAlias { get; set; } 

        [Required]
        public DateTime DateOfBirth { get; set; } 
        public int TotalGamesPlayed { get; set; } = 0;
        public int TotalWins { get; set; } = 0;
        public int BestScore { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal AverageScore { get; set; } = 0.00m;

        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public Wallet Wallet { get; set; }

    }
}