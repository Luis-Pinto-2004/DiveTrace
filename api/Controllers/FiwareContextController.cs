using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/fiware")]
public sealed class FiwareContextController : ControllerBase
{
    private readonly IFiwareContextService _fiware;

    public FiwareContextController(IFiwareContextService fiware)
    {
        _fiware = fiware;
    }

    [HttpGet("context")]
    public async Task<IActionResult> CurrentContext(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.FiwareView))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.FiwareView);
        }

        return Ok(await _fiware.GetContextAsync(cancellationToken));
    }

    [HttpPost("publish-current")]
    public async Task<IActionResult> PublishCurrentContext(CancellationToken cancellationToken)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.FiwareManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.FiwareManage);
        }

        return Ok(await _fiware.PublishCurrentContextAsync(cancellationToken));
    }
}
