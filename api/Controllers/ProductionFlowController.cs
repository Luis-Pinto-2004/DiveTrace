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
        return Ok(await _flow.GetFlowSummaryAsync(cancellationToken));
    }

    [HttpGet("operator/workbench")]
    public async Task<IActionResult> OperatorWorkbench(CancellationToken cancellationToken)
    {
        return Ok(await _flow.GetOperatorWorkbenchAsync(cancellationToken));
    }

    [HttpGet("customer/orders/{publicTrackingCode}")]
    public async Task<IActionResult> CustomerOrder(string publicTrackingCode, CancellationToken cancellationToken)
    {
        var order = await _flow.GetCustomerOrderAsync(publicTrackingCode, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }
}
