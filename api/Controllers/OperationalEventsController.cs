using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/operational-events")]
public sealed class OperationalEventsController : ControllerBase
{
    private readonly OperationalEventService _events;

    public OperationalEventsController(OperationalEventService events)
    {
        _events = events;
    }

    [HttpGet]
    public async Task<IActionResult> GetOperationalEvents(
        [FromQuery] string? eventType,
        [FromQuery] int? productUnitId,
        [FromQuery] int? supportId,
        [FromQuery] int? manufacturingOrderId,
        [FromQuery] bool? isDemo,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.OperationalEventsView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.OperationalEventsView);
        }

        return Ok(await _events.QueryAsync(new OperationalEventQueryRequest
        {
            EventType = eventType,
            ProductUnitId = productUnitId,
            SupportId = supportId,
            ManufacturingOrderId = manufacturingOrderId,
            IsDemo = isDemo,
            From = from,
            To = to,
            Limit = limit <= 0 ? 100 : limit
        }, cancellationToken: cancellationToken));
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.OperationalEventsView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.OperationalEventsView);
        }

        return Ok(await _events.GetRecentAsync(limit <= 0 ? 20 : limit, cancellationToken));
    }

    [HttpGet("/api/product-units/{id:int}/events")]
    public async Task<IActionResult> GetProductUnitEvents(int id, [FromQuery] int limit, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsTrace))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsTrace);
        }

        return Ok(await _events.GetForProductUnitAsync(id, limit <= 0 ? 200 : limit, cancellationToken));
    }
}
