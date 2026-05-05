using DriveTraceCore.Api.Models;
using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly DemoEventService _events;

    public EventsController(DemoEventService events)
    {
        _events = events;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> InjectManualEvent([FromBody] ManualEventRequest request)
    {
        try
        {
            return Ok(await _events.InjectManualEventAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("playback")]
    public async Task<IActionResult> ExecutePlayback([FromBody] PlaybackRequest request)
    {
        return Ok(await _events.ExecutePlaybackAsync(request));
    }
}
