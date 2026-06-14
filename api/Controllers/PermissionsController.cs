using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly PermissionCatalogService _permissions;

    public PermissionsController(PermissionCatalogService permissions)
    {
        _permissions = permissions;
    }

    [HttpGet("catalog")]
    public IActionResult GetCatalog([FromQuery] string? role)
    {
        var requestedRole = role ?? Request.Headers["X-DriveTrace-Role"].FirstOrDefault();
        return Ok(_permissions.GetCatalog(requestedRole));
    }

    [HttpGet("profiles/{role}")]
    public IActionResult GetRoleProfile(string role)
    {
        var activeRole = PermissionCatalogService.ResolveRole(role);
        return Ok(new
        {
            role = activeRole,
            permissions = PermissionCatalogService.GetPermissionsForRole(activeRole).OrderBy(x => x)
        });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentPermissions()
    {
        var role = PermissionCatalogService.ResolveRole(HttpContext);
        return Ok(new
        {
            role,
            permissions = PermissionCatalogService.ResolvePermissions(HttpContext).OrderBy(x => x)
        });
    }
}
