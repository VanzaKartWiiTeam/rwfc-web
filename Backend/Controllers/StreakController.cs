using Microsoft.AspNetCore.Mvc;
using RetroRewindWebsite.Models.DTOs.Streak;
using RetroRewindWebsite.Services.Application;

namespace RetroRewindWebsite.Controllers;

/// <summary>
/// Daily play streaks. Reads are public; restoring a streak and toggling vacation live under
/// <c>/api/moderation</c> so only the bot (Bearer token) can call them, after it has checked
/// that the Discord user owns the profile.
/// A rule violation (no restores left, nothing to restore, ...) is a 200 with Success = false and an Error code.
/// </summary>
[ApiController]
public class StreakController : ControllerBase
{
    private readonly IStreakService _streakService;
    private readonly ILogger<StreakController> _logger;

    private const int MaxTopLimit = 50;

    public StreakController(IStreakService streakService, ILogger<StreakController> logger)
    {
        _streakService = streakService;
        _logger = logger;
    }

    [HttpGet("api/streak/{pid:long}")]
    [ProducesResponseType<StreakDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StreakDto>> GetStreak(long pid)
    {
        try
        {
            var streak = await _streakService.GetStreakAsync(pid);
            if (streak == null)
                return NotFound($"No streak for player with PID '{pid}'");

            return Ok(streak);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streak of {Pid}", pid);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the streak");
        }
    }

    [HttpGet("api/streak/top")]
    [ProducesResponseType<List<StreakDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StreakDto>>> GetTopStreaks([FromQuery] int limit = 10)
    {
        try
        {
            return Ok(await _streakService.GetTopStreaksAsync(Math.Clamp(limit, 1, MaxTopLimit)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top streaks");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the top streaks");
        }
    }

    [HttpPost("api/moderation/streak/restore")]
    [ProducesResponseType<StreakActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StreakActionResultDto>> Restore([FromBody] StreakRestoreRequest request)
    {
        try
        {
            var result = await _streakService.RestoreAsync(request.Pid);
            if (result == null)
                return NotFound($"No streak for player with PID '{request.Pid}'");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring streak of {Pid}", request.Pid);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while restoring the streak");
        }
    }

    [HttpPost("api/moderation/streak/vacation")]
    [ProducesResponseType<StreakActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StreakActionResultDto>> SetVacation([FromBody] StreakVacationRequest request)
    {
        try
        {
            var result = await _streakService.SetVacationAsync(request.Pid, request.Enabled);
            if (result == null)
                return NotFound($"No streak for player with PID '{request.Pid}'");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting vacation of {Pid}", request.Pid);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while setting the vacation");
        }
    }
}
