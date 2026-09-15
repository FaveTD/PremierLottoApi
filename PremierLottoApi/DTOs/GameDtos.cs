using PremierLottoApi.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace PremierLottoApi.DTOs

{
    public class PlayGameDto
    {
        [Required]
        public string PlayerAlias{ get; set; }

        [Required]
        public GameLevel GameType { get; set; } 

        [Range(200, double.MaxValue, ErrorMessage = "Minimum stake amount is 200.")]
        public decimal StakeAmount { get; set; }
    }

    public class SubmitGuessDto
    {
        public int RoundId { get; set; }
        public string GameMode { get; set; }
        public string GuessValue { get; set; }
    }
    public class RegisterPlayerDto
    {
        [Required(ErrorMessage = "Legal name is required.")]
        public string LegalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Player alias is required.")]
        public string PlayerAlias { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format. Please use the YYYY-MM-DD format (e.g., 2004-05-18).")]
        public DateTime DateOfBirth { get; set; }
    }
    public class GuessRequestDto
    {
        public string Guesses { get; set; } 
    }
    public class CreatePlayerDto
    {
        public string LegalName { get; set; }
        public string PlayerAlias { get; set; }
    }
    public class WalletResponseDto
    {
        public decimal Balance { get; set; }
        public decimal DebtOwed { get; set; }
    }
    public class CreateGamePoolDto
    {
        public GameLevel GameType { get; set; }
        public string PlayerAlias { get; set; } = string.Empty;
        [Range(200, double.MaxValue, ErrorMessage = "Minimum stake amount is 200.")]
        public decimal StakeAmount { get; set; }
    }
    
}