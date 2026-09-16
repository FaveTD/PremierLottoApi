using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierLottoApi.DTOs;
using PremierLottoApi.Models;
using PremierLottoApi.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PremierLottoApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GameplayController : ControllerBase
    {
        private readonly IGameSessionService _gameSessionService;

        public GameplayController(IGameSessionService gameSessionService)
        {
            _gameSessionService = gameSessionService;
        }

        /// <summary>
        /// Automatically enters a player into a matching game pool using their stake and chosen game type.
        /// </summary>
        [HttpPost("enter")]
        public async Task<IActionResult> StakeGame([FromBody] PlayGameDto dto)
        {
            try
            {
                var result = await _gameSessionService.ProcessGameStakeAsync(dto.PlayerAlias, dto.GameType.ToString(), dto.StakeAmount);

                return StatusCode(201, new
                {
                    message = $"Game successfully staked and added to pool #{result.PoolId}",
                    gameType = dto.GameType.ToString(),
                    stakeAmount = dto.StakeAmount,
                    poolId = result.PoolId,
                    poolStatus = result.PoolStatus
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Submits the player's guesses for the current round and checks if it's their turn or triggers a tie-breaker.
        /// </summary>
        [HttpPost("submit-guesses")]
        public async Task<IActionResult> SubmitGuesses(int poolId, [FromQuery] string playerAlias, [FromBody] GuessRequestDto request)
        {
            try
            {
                var (playerGuess, roundNumber, status, isTieBreaker) = await _gameSessionService.SubmitRoundGuessesAsync(poolId, playerAlias, request.Guesses);
                string customMessage = status switch
                {
                    "Completed" => "Game session completed! Winners have been calculated and payouts distributed.",
                    "Active" when isTieBreaker => $"Scores are tied! Sudden-death tie-breaker activated. Starting Round {roundNumber}!",
                    _ => "Guesses submitted successfully! Round checked and updated."
                };

                return Ok(new
                {
                    message = "Guesses submitted successfully! Round checked and updated.",
                    matches = playerGuess.MatchesCount,
                    roundNumber = roundNumber,
                    passedThreshold = playerGuess.MetThreshold,
                    sessionStatus = status,
                    isTieBreaker = isTieBreaker
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Calculates the final winners for the pool session and distributes the prize payouts.
        /// </summary>
        [Authorize(AuthenticationSchemes = "ApiKey")]
        [HttpPost("distribute-prizes")]
        public async Task<IActionResult> ForceDistributePrizes(int poolId)
        {
            try
            {
                var (winners, rollOverAmount) = await _gameSessionService.CalculateWinnersAndDistributePayoutsAsync(poolId);

                if(rollOverAmount > 0)
                {
                    return Ok(new
                    {
                        status = "Rollover",
                        message = $"Session #{poolId} evaluated, but no players met the winning threshold. The prize pool of ₦{rollOverAmount:N2} will roll over to the next session.",
                        rolledOverAmount = rollOverAmount
                    });
                }
                if (winners.Count == 0)
                {
                    return Ok(new
                    {
                        message = $"Session #{poolId} evaluated, but no players met the winning threshold."
                    });
                }

                return Ok(new
                {
                    message = $"Prizes successfully calculated and distributed for session #{poolId}!",
                    totalWinners = winners.Count,
                    winners = winners 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}