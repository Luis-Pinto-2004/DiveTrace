using DriveTraceCore.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/traceability")]
public sealed class TraceabilityController : ControllerBase
{
    private readonly DriveTraceDbContext _db;

    public TraceabilityController(DriveTraceDbContext db)
    {
        _db = db;
    }

    [HttpGet("product-units/{id:int}")]
    public async Task<IActionResult> ProductUnitTrace(int id)
    {
        var unit = await _db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (unit is null)
        {
            return NotFound();
        }

        var assignments = await _db.UnitSupportAssignments
            .AsNoTracking()
            .Where(x => x.ProductUnitId == id)
            .OrderBy(x => x.DateTimeIn)
            .ToListAsync();

        var supportIds = assignments.Select(x => x.SupportId).Distinct().ToArray();
        var movements = await _db.SupportLocalizationHistory
            .AsNoTracking()
            .Where(x => supportIds.Contains(x.SupportId))
            .OrderBy(x => x.DateTime)
            .Join(_db.Supports, h => h.SupportId, s => s.Id, (h, s) => new { h, s })
            .Join(_db.ProductionLineSections, hs => hs.h.SectionId, sec => sec.Id, (hs, sec) => new
            {
                supportCode = hs.s.SupportCode,
                section = sec.Name,
                eventType = hs.h.EventType,
                dateTime = hs.h.DateTime
            })
            .ToListAsync();

        var materialUsage = await _db.UnitMaterialLotUsages
            .AsNoTracking()
            .Where(x => x.ProductUnitId == id)
            .Join(_db.LotRawMaterials, usage => usage.LotId, lot => lot.Id, (usage, lot) => new { usage, lot })
            .Join(_db.RawMaterials, ul => ul.lot.RawMaterialId, mat => mat.Id, (ul, mat) => new
            {
                rawMaterial = mat.Name,
                lotNumber = ul.lot.LotNumber,
                ul.usage.AssociationType,
                ul.usage.Quantity,
                unit = ul.lot.LotUnit
            })
            .ToListAsync();

        var quality = await _db.QualityResults.AsNoTracking().Where(x => x.ProductUnitId == id).OrderBy(x => x.RecordedAt).ToListAsync();
        var nonconformities = await _db.Nonconformities.AsNoTracking().Where(x => x.ProductUnitId == id).OrderBy(x => x.CreatedAt).ToListAsync();
        var rework = await _db.ReworkRecords.AsNoTracking().Where(x => x.ProductUnitId == id).OrderBy(x => x.StartedAt).ToListAsync();
        var scrap = await _db.ScrapRecords.AsNoTracking().Where(x => x.ProductUnitId == id).OrderBy(x => x.ScrappedAt).ToListAsync();

        return Ok(new
        {
            unit,
            supportAssignments = assignments,
            movements,
            materialUsage,
            quality,
            nonconformities,
            rework,
            scrap
        });
    }
}
