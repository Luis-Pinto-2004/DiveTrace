using DriveTraceCore.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DriveTraceDbContext _db;

    public DashboardController(DriveTraceDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var activeUnits = await _db.ProductUnits.CountAsync(x => x.Status == "Active" || x.Status == "Rework" || x.Status == "Blocked");
        var qualityIssues = await _db.Nonconformities.CountAsync(x => x.Status != "Closed");
        var activeSupports = await _db.Supports.CountAsync(x => x.Status != "Available");
        var openOrders = await _db.ManufacturingOrders.CountAsync(x => x.Status != "Completed");
        var rackAssignments = await _db.RackSupportAssignments.CountAsync(x => x.DateTimeOut == null);

        var sections = await _db.ProductionLineSections
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                sectionId = x.Id,
                section = x.Name,
                sectionCode = x.SectionCode,
                sectionType = x.SectionType,
                activeSupports = _db.Supports.Count(s => s.CurrentSectionId == x.Id),
                productUnits = _db.ProductUnits.Count(u => u.CurrentSectionId == x.Id)
            })
            .ToListAsync();

        var recentEvents = await _db.SupportLocalizationHistory
            .AsNoTracking()
            .OrderByDescending(x => x.DateTime)
            .Take(12)
            .Join(_db.Supports, h => h.SupportId, s => s.Id, (h, s) => new { h, s })
            .Join(_db.ProductionLineSections, hs => hs.h.SectionId, sec => sec.Id, (hs, sec) => new
            {
                supportCode = hs.s.SupportCode,
                section = sec.Name,
                eventType = hs.h.EventType,
                dateTime = hs.h.DateTime
            })
            .ToListAsync();

        var qualityAlerts = await _db.Nonconformities
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Join(_db.ProductUnits, nc => nc.ProductUnitId, u => u.Id, (nc, unit) => new
            {
                unitCode = unit.UnitCode,
                nc.Severity,
                nc.Status,
                nc.Description,
                nc.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            appName = "DriveTrace Core",
            subtitle = "DRIVOLUTION WP3 — WIP Traceability and Monitoring Platform",
            generatedAt = DateTime.UtcNow,
            counts = new
            {
                openOrders,
                activeUnits,
                activeSupports,
                qualityIssues,
                rackAssignments
            },
            wipBySection = sections,
            recentEvents,
            qualityAlerts
        });
    }
}
