using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly DriveTraceDbContext _db;

    public AuthController(DriveTraceDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var profile = PermissionCatalogService.ResolveDemoUser(HttpContext);
        var role = PermissionCatalogService.ResolveRole(HttpContext);
        if (string.IsNullOrWhiteSpace(Request.Headers["X-DriveTrace-Role"].FirstOrDefault()))
        {
            role = profile.Role;
        }

        var line = string.IsNullOrWhiteSpace(profile.LineCode)
            ? null
            : await _db.ProductionLines.AsNoTracking().FirstOrDefaultAsync(x => x.LineCode == profile.LineCode, cancellationToken);
        var section = string.IsNullOrWhiteSpace(profile.SectionCode)
            ? null
            : await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.SectionCode == profile.SectionCode, cancellationToken);
        var customer = string.IsNullOrWhiteSpace(profile.CustomerCode)
            ? null
            : await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerCode == profile.CustomerCode, cancellationToken);

        return Ok(new
        {
            id = profile.Username,
            profile.Name,
            username = profile.Username,
            profile.Email,
            role,
            roleKey = PermissionCatalogService.ToRoleKey(role),
            permissions = PermissionCatalogService.GetPermissionsForRole(role).OrderBy(x => x),
            preferredLanguage = "pt-PT",
            preferredTheme = "light",
            department = profile.Department,
            associatedEntity = ResolveAssociatedEntity(profile, line?.Id, section?.Id, customer?.Id),
            customer = customer is null ? null : new
            {
                customer.Id,
                customer.CustomerCode,
                customer.Name,
                defaultPublicTrackingCode = "TRC-PORTA-001"
            },
            assignedLine = line is null ? null : new { line.Id, code = line.LineCode, line.Name },
            assignedSection = section is null ? null : new { section.Id, code = section.SectionCode, section.Name, section.SectionType }
        });
    }

    [HttpGet("demo-users")]
    public IActionResult DemoUsers()
    {
        return Ok(PermissionCatalogService.DemoUsers.Select(user => new
        {
            id = user.Username,
            user.Name,
            username = user.Username,
            password = user.Password,
            role = user.Role,
            roleKey = PermissionCatalogService.ToRoleKey(user.Role),
            user.Email,
            user.Department,
            user.LineCode,
            user.SectionCode,
            user.CustomerCode,
            isDemo = true
        }));
    }

    private static object? ResolveAssociatedEntity(DemoUserProfile profile, int? lineId, int? sectionId, int? customerId)
    {
        if (!string.IsNullOrWhiteSpace(profile.CustomerCode))
        {
            return new { type = "Customer", id = customerId, code = profile.CustomerCode };
        }

        if (!string.IsNullOrWhiteSpace(profile.SectionCode))
        {
            return new { type = "ProductionLineSection", id = sectionId, code = profile.SectionCode };
        }

        if (!string.IsNullOrWhiteSpace(profile.LineCode))
        {
            return new { type = "ProductionLine", id = lineId, code = profile.LineCode };
        }

        return null;
    }
}
