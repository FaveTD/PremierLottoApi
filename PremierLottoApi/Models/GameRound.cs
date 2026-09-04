namespace PremierLottoApi.Models
{
    public class GameRound
    {
        public int Id { get; set; }
        public int GameSessionId { get; set; }
        public int RoundNumber { get; set; } 
        public string WinningValues { get; set; } 
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    }
}
