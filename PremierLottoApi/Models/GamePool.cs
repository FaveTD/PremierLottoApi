using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PremierLottoApi.Models
{
    public class GamePool
    {
        [Key]
        public int Id { get; set; }
        public string GameType { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrizePool { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByAlias { get; set; } = string.Empty;

        public ICollection<GamePoolParticipant> Participants { get; set; } = new List<GamePoolParticipant>();
    }
}