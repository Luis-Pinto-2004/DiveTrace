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
        var requiredPermission = RequiredManualEventPermission(request.EventType);
        if (!PermissionCatalogService.HasPermission(HttpContext, requiredPermission))
        {
            return PermissionCatalogService.Forbidden(requiredPermission);
        }

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
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.SimulationManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationManage);
        }

        return Ok(await _events.ExecutePlaybackAsync(request));
    }

    private static string RequiredManualEventPermission(string? eventType)
    {
        if (eventType?.Equals("RegisterQualityResult", StringComparison.OrdinalIgnoreCase) == true)
        {
            return PermissionNames.QualityRecord;
        }

        if (eventType?.Equals("TransferSupportToRack", StringComparison.OrdinalIgnoreCase) == true)
        {
            return PermissionNames.RacksManage;
        }

        return PermissionNames.ProductUnitsTransfer;
    }
}
