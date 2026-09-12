using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierLottoApi.Data;
using PremierLottoApi.DTOs;
using PremierLottoApi.Models;
using PremierLottoApi.Services;
using System.Security.Claims;

namespace PremierLottoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public ExternalAuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("signin-google")]
        public IActionResult SignInGoogle()
        {
            var redirectUrl = Url.Action(nameof(CompleteGoogleLogin), "ExternalAuth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            properties.Items.Add("prompt", "select_account");

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("complete-google-login")]
        public async Task<IActionResult> CompleteGoogleLogin()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return BadRequest("External authentication failed.");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = new byte[0], 
                    PasswordSalt = new byte[0]
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtService.RefreshTokenDays());
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
    }
}
