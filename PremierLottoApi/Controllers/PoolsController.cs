using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PremierLottoApi.DTOs;
using PremierLottoApi.Services.Interfaces;

namespace PremierLottoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoolsController(IGameSessionService gameSessionService) : ControllerBase
    {
        private readonly IGameSessionService _gameSessionService = gameSessionService;

        /// <summary>
        /// Creates a new game pool and automatically adds the creator as the first participant.
        /// </summary>
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePool([FromBody] CreateGamePoolDto dto)
        {
            try
            {
               var result = await _gameSessionService.CreateGamePoolAsync(dto.GameType.ToString(), dto.PlayerAlias, dto.StakeAmount);

                return StatusCode(201, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Allows a player to join an existing game pool by providing their alias and stake amount.
        /// </summary>
        [Authorize]
        [HttpPost("join")]
        public async Task<IActionResult> JoinPool(int poolId, [FromBody] PlayGameDto dto)
        {
            try
            {
                dynamic result = await _gameSessionService.JoinSpecificPoolAsync(poolId, dto.PlayerAlias, dto.StakeAmount);

                return Ok(new
                {
                    message = $"Successfully joined game pool #{result.poolId}!",
                    PoolId = result.poolId,
                    PoolStatus = result.poolStatus,
                    stakeAmount = dto.StakeAmount,
                    CreatedByAlias = result.createdByAlias
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Closes a game pool early and starts the game session, restricted to authorized players.
        /// </summary>
        [Authorize]
        [HttpPost("start")]
        public async Task<IActionResult> ClosePoolEarly(int poolId, [FromQuery] string playerAlias)
        {
            try
            {
                var result = await _gameSessionService.ClosePoolEarlyAsync(poolId, playerAlias);
                return Ok(new
                {
                    poolId = result.PoolId,
                    status = result.PoolStatus,
                    message = result.Message,
                    totalPrizePool = result.TotalPrizePool
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all game pools, with an optional filter for their current status.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllPools([FromQuery] string? status)
        {
            try
            {
                var pools = await _gameSessionService.GetAllPoolsWithParticipantsAsync(status);
                return Ok(pools);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all game pools associated with a specific player alias, also with an optional filter.
        /// </summary>
        [Authorize]
        [HttpGet("{playerAlias}")]
        public async Task<IActionResult> GetPlayerPools(string playerAlias, [FromQuery] string? status)
        {
            try
            {
                var pools = await _gameSessionService.GetPoolsByPlayerAliasAsync(playerAlias, status);
                return Ok(pools);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}

