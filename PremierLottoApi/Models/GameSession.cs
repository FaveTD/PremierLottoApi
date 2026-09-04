namespace PremierLottoApi.Models
{
    public class GameSession
    {
        public int Id { get; set; }
        public int PoolId { get; set; }
        public string GameType { get; set; } 
        public int TotalRounds { get; set; } 
        public int CurrentRound { get; set; } = 1;
        public string Status { get; set; } = "Active"; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
