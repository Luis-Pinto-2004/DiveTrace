using DriveTraceCore.Api.Models;
using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/reconditioning")]
public sealed class ReconditioningController : ControllerBase
{
    private readonly ReconditioningService _service;

    public ReconditioningController(ReconditioningService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ReconditioningListDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? line,
        [FromQuery] string? section,
        [FromQuery] string? severity,
        [FromQuery] string? quality,
        CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.ReconditioningRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ReconditioningRead);
        }

        var query = new ReconditioningQuery(status, line, section, severity, quality);
        return Ok(await _service.GetListAsync(query, AccessContext(), cancellationToken));
    }

    [HttpGet("candidates")]
    public async Task<ActionResult<ReconditioningListDto>> GetCandidates(CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.ReconditioningRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ReconditioningRead);
        }

        return Ok(await _service.GetCandidatesAsync(AccessContext(), cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReconditioningItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.ReconditioningRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ReconditioningRead);
        }

        var item = await _service.GetByIdAsync(id, AccessContext(), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("/api/product-units/{id:int}/mark-reconditionable")]
    public async Task<ActionResult<ReconditioningItemDto>> MarkRecoverable(
        int id,
        [FromBody] ReconditioningActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.ReconditioningWrite))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ReconditioningWrite);
        }

        return await RunDecisionAsync(() => _service.MarkRecoverableAsync(id, request, AccessContext(), cancellationToken));
    }

    [HttpPost("/api/product-units/{id:int}/complete-reconditioning")]
    public async Task<ActionResult<ReconditioningItemDto>> Complete(
        int id,
        [FromBody] ReconditioningActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.ReconditioningDecide))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ReconditioningDecide);
        }

        return await RunDecisionAsync(() => _service.CompleteAsync(id, request, AccessContext(), cancellationToken));
    }

    [HttpPost("/api/product-units/{id:int}/reject-reconditioning")]
    public async Task<ActionResult<ReconditioningItemDto>> Reject(
        int id,
        [FromBody] ReconditioningRejectRequest request,
        CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.ReconditioningDecide))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.ReconditioningDecide);
        }

        return await RunDecisionAsync(() => _service.RejectAsync(id, request, AccessContext(), cancellationToken));
    }

    private bool Can(string permission)
    {
        return PermissionCatalogService.HasPermission(HttpContext, permission);
    }

    private ReconditioningAccessContext AccessContext()
    {
        var profile = PermissionCatalogService.ResolveDemoUser(HttpContext);
        return new ReconditioningAccessContext(
            profile.Username,
            profile.Role,
            profile.LineCode,
            profile.SectionCode,
            profile.CustomerCode);
    }

    private static async Task<ActionResult<ReconditioningItemDto>> RunDecisionAsync(Func<Task<ReconditioningItemDto>> action)
    {
        try
        {
            return await action();
        }
        catch (KeyNotFoundException ex)
        {
            return new NotFoundObjectResult(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}
