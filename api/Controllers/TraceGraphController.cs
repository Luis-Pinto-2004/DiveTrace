using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/trace-graph")]
public sealed class TraceGraphController : ControllerBase
{
    private readonly TraceGraphService _traceGraph;

    public TraceGraphController(TraceGraphService traceGraph)
    {
        _traceGraph = traceGraph;
    }

    [HttpGet("factory")]
    public async Task<IActionResult> Factory(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsView);
        }

        return Ok(await _traceGraph.GetFactoryGraphAsync(BuildAccessContext(), cancellationToken));
    }

    [HttpGet("product-unit/{id:int}")]
    public async Task<IActionResult> ProductUnit(int id, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsTrace))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsTrace);
        }

        try
        {
            var graph = await _traceGraph.GetProductUnitGraphAsync(id, BuildAccessContext(), cancellationToken);
            return graph is null ? NotFound() : Ok(graph);
        }
        catch (UnauthorizedAccessException)
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsTrace);
        }
    }

    [HttpGet("manufacturing-order/{id:int}")]
    public async Task<IActionResult> ManufacturingOrder(int id, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.OrdersView)
            && !PermissionCatalogService.HasPermission(HttpContext, PermissionNames.CustomerPortalView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.OrdersView);
        }

        try
        {
            var graph = await _traceGraph.GetManufacturingOrderGraphAsync(id, BuildAccessContext(), cancellationToken);
            return graph is null ? NotFound() : Ok(graph);
        }
        catch (UnauthorizedAccessException)
        {
            return PermissionCatalogService.Forbidden(PermissionNames.CustomerPortalView);
        }
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsView)
            && !PermissionCatalogService.HasPermission(HttpContext, PermissionNames.OrdersView)
            && !PermissionCatalogService.HasPermission(HttpContext, PermissionNames.CustomerPortalView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsView);
        }

        return Ok(await _traceGraph.GetOptionsAsync(BuildAccessContext(), cancellationToken));
    }

    [HttpGet("fiware-status")]
    public async Task<IActionResult> FiwareStatus(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.FiwareView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.FiwareView);
        }

        return Ok(await _traceGraph.GetFiwareGraphStatusAsync(cancellationToken));
    }

    private TraceGraphAccessContext BuildAccessContext()
    {
        var profile = PermissionCatalogService.ResolveDemoUser(HttpContext);
        var role = PermissionCatalogService.ResolveRole(HttpContext);
        return new TraceGraphAccessContext(
            role,
            PermissionCatalogService.ToRoleKey(role),
            profile.Username,
            profile.LineCode,
            profile.SectionCode,
            profile.CustomerCode);
    }
}
