using PremierLottoApi.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace PremierLottoApi.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; } = null;
        public string Password { get; set; } = null;
    }
    public class LoginDto
    {
        public string Email { get; set; } = null;
        public string Password { get; set; } = null;
    }
    public class RefreshDto
    {
        public string RefreshToken { get; set; } = null;

    }
    public class AuthResponse
    {
        public string AccessToken { get; set; } = null;
        public string RefreshToken { get; set; } = null;
    }
        
}
