using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierLottoApi.Data;
using PremierLottoApi.DTOs;
using PremierLottoApi.Models;
using PremierLottoApi.Services.Interfaces;
using System.Numerics;
namespace PremierLottoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController(AppDbContext context, IGameSessionService gameSessionService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IGameSessionService _gameSessionService = gameSessionService;

        /// <summary>
        /// Registers a new player after verifying they meet the minimum age requirement of 18.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAndVerifyAge([FromBody] RegisterPlayerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid input or date format. Please ensure your date of birth follows the YYYY-MM-DD format (e.g., 2004-05-18)."
                });
            }
            var today = DateTime.Today;
            int age = today.Year - dto.DateOfBirth.Year;
            if (dto.DateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                return BadRequest(new { message = $"Access denied. You are {age} years old. You must be 18 or older to play." });
            }

            try
            {
                await _gameSessionService.RegisterPlayerAsync(dto.LegalName, dto.PlayerAlias, dto.DateOfBirth);

                return Created(string.Empty, new
                {
                    message = "Age verified successfully! Welcome to Premier Lotto.",
                    playerAlias = dto.PlayerAlias
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// Retrieves a complete list of all registered players.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllPlayers()
        {
            var players = await _context.Players.ToListAsync();
            return Ok(players);
        }

        /// <summary>
        /// Retrieves a specific player's profile by their unique ID.
        /// </summary>
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPlayerById(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound(new { message = $"Player with ID {id} was not found." });
            }
            return Ok(player);
        }

        /// <summary>
        /// Retrieves a specific player's profile using their unique player alias.
        /// </summary>
        [Authorize]
        [HttpGet("{playerAlias}")]
        public async Task<IActionResult> GetPlayerByAlias(string playerAlias)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());
            if(player==null)
            {
                return NotFound(new { message = $"Player with alias {playerAlias} was not found." });
            }
            return Ok(player);
        }

        /// <summary>
        /// Checks and retrieves the current wallet balance and debt owed for a specific player alias.
        /// </summary>
        [Authorize]
        [HttpGet("wallet")]
        public async Task<IActionResult> GetWalletByPlayerAlias(string playerAlias)
        {
            var walletDto = await _context.Players
            .Where(p => p.PlayerAlias.ToLower() == playerAlias.ToLower())
            .Select(p => new WalletResponseDto
            {
                Balance = p.Wallet.Balance,
                DebtOwed = p.Wallet.DebtOwed
            })
            .FirstOrDefaultAsync();

            if (walletDto == null)
            {
                return NotFound(new { message = "Wallet not found for this player." });
            }

            return Ok(walletDto);
        }

        /// <summary>
        /// Deletes a specific player account using their unique ID.
        /// </summary>
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound(new { message = $"Player with ID {id} not found." });
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Player '{player.PlayerAlias}' (ID: {id}) has been deleted successfully." });
        }

        /// <summary>
        /// Deletes a specific player account using their unique user alias.
        /// </summary>
        [Authorize]
        [HttpDelete("{playerAlias}")]
        public async Task<IActionResult> DeletePlayerByAlias(string playerAlias)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerAlias.ToLower() == playerAlias.ToLower());

            if (player == null)
            {
                return NotFound(new { message = $"Player with alias '{playerAlias}' was not found." });
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Player '{player.PlayerAlias}' has been deleted successfully." });
        }

        /// <summary>
        /// Deletes all registered player accounts from the database.
        /// </summary>
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteAllPlayers()
        {
            var players = await _context.Players.ToListAsync();
            if (!players.Any())
            {
                return NotFound(new { message = "No players found to delete." });
            }

            _context.Players.RemoveRange(players);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"All players have been deleted successfully." });
        }

        
    }
}
