using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PremierLottoApi.Models
{
    public class GamePoolParticipant
    {
        [Key]
        public int Id { get; set; }
        public int PoolId { get; set; }
        public int PlayerId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal StakeAmount { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public GamePool Pool { get; set; } = null!;
        public Player Player { get; set; } = null!;
    }
}