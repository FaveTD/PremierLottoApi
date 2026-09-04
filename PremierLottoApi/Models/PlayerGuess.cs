namespace PremierLottoApi.Models
{
    public class PlayerGuess
    {
        public int Id { get; set; }
        public int GameSessionId { get; set; }
        public int RoundId { get; set; }
        public int PlayerId { get; set; }
        public string Guesses { get; set; } 
        public int MatchesCount { get; set; } 
        public bool MetThreshold { get; set; } 
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
