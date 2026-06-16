using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ProductionFlowController : ControllerBase
{
    private readonly ProductionFlowService _flow;

    public ProductionFlowController(ProductionFlowService flow)
    {
        _flow = flow;
    }

    [HttpPost("product-units/{id:int}/transfer")]
    public async Task<IActionResult> TransferProductUnit(int id, [FromBody] ProductUnitTransferRequest request, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsTransfer))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsTransfer);
        }

        try
        {
            return Ok(await _flow.TransferAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("product-units/{id:int}/trace")]
    public async Task<IActionResult> ProductUnitTrace(int id, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsTrace))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsTrace);
        }

        var trace = await _flow.GetTraceAsync(id, cancellationToken);
        return trace is null ? NotFound() : Ok(trace);
    }

    [HttpGet("operations/flow-summary")]
    public async Task<IActionResult> FlowSummary(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsView);
        }

        return Ok(await _flow.GetFlowSummaryAsync(cancellationToken));
    }

    [HttpGet("operator/workbench")]
    public async Task<IActionResult> OperatorWorkbench(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.ProductUnitsView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ProductUnitsView);
        }

        return Ok(await _flow.GetOperatorWorkbenchAsync(cancellationToken));
    }

    [HttpGet("customer/orders/{publicTrackingCode}")]
    public async Task<IActionResult> CustomerOrder(string publicTrackingCode, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.CustomerPortalView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.CustomerPortalView);
        }

        var profile = PermissionCatalogService.ResolveDemoUser(HttpContext);
        var role = PermissionCatalogService.ResolveRole(HttpContext);
        var customerCode = role.Equals(RoleNames.Customer, StringComparison.OrdinalIgnoreCase) ? profile.CustomerCode : null;
        var order = await _flow.GetCustomerOrderAsync(publicTrackingCode, customerCode, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("customer/orders")]
    public async Task<IActionResult> CustomerOrders(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.CustomerPortalView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.CustomerPortalView);
        }

        var profile = PermissionCatalogService.ResolveDemoUser(HttpContext);
        var role = PermissionCatalogService.ResolveRole(HttpContext);
        var customerCode = role.Equals(RoleNames.Customer, StringComparison.OrdinalIgnoreCase) ? profile.CustomerCode : null;
        return Ok(await _flow.GetCustomerOrdersAsync(customerCode, cancellationToken));
    }

    [HttpPost("customer/orders")]
    public async Task<IActionResult> CreateCustomerOrder([FromBody] CustomerOrderCreateRequest request, CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.CustomerPortalView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.CustomerPortalView);
        }

        var profile = PermissionCatalogService.ResolveDemoUser(HttpContext);
        if (string.IsNullOrWhiteSpace(profile.CustomerCode))
        {
            return BadRequest(new { error = "O perfil autenticado não está associado a um cliente." });
        }

        try
        {
            return Ok(await _flow.CreateCustomerOrderAsync(profile.CustomerCode, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
