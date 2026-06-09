using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public sealed class DemoEventService
{
    private readonly DriveTraceDbContext _db;

    public DemoEventService(DriveTraceDbContext db)
    {
        _db = db;
    }

    public async Task<object> InjectManualEventAsync(ManualEventRequest request)
    {
        var eventType = request.EventType.Trim();

        if (eventType.Equals("MoveSupport", StringComparison.OrdinalIgnoreCase))
        {
            return await MoveSupportAsync(request);
        }

        if (eventType.Equals("RegisterQualityResult", StringComparison.OrdinalIgnoreCase))
        {
            return await RegisterQualityAsync(request);
        }

        if (eventType.Equals("TransferSupportToRack", StringComparison.OrdinalIgnoreCase))
        {
            return await TransferToRackAsync(request);
        }

        throw new InvalidOperationException("Unsupported event type. Supported values: MoveSupport, RegisterQualityResult, TransferSupportToRack.");
    }

    public async Task<object> ExecutePlaybackAsync(PlaybackRequest request)
    {
        var supports = await _db.Supports.OrderBy(x => x.SupportCode).ToListAsync();
        var sections = await _db.ProductionLineSections.OrderBy(x => x.Id).ToListAsync();
        var events = new List<object>();

        foreach (var support in supports)
        {
            var currentIndex = support.CurrentSectionId is null
                ? -1
                : sections.FindIndex(x => x.Id == support.CurrentSectionId.Value);
            var nextIndex = Math.Min(currentIndex + 1, sections.Count - 1);
            var nextSection = sections[nextIndex];
            support.CurrentSectionId = nextSection.Id;

            var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.CurrentSupportId == support.Id);
            if (unit is not null)
            {
                unit.CurrentSectionId = nextSection.Id;
                if (nextSection.SectionType is "Post-line Logistics" or "Logística Pós-Linha")
                {
                    unit.Status = unit.QualityStatus == "FAIL" ? unit.Status : "Completed";
                    unit.CompletedAt ??= DateTime.UtcNow;
                }
            }

            _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
            {
                SupportId = support.Id,
                SectionId = nextSection.Id,
                DateTime = DateTime.UtcNow,
                EventType = "PlaybackMovement"
            });

            events.Add(new { support.SupportCode, section = nextSection.Name, unit = unit?.UnitCode });
        }

        await _db.SaveChangesAsync();
        return new { request.Scenario, executedAt = DateTime.UtcNow, events };
    }

    private async Task<object> MoveSupportAsync(ManualEventRequest request)
    {
        var support = await _db.Supports.FirstOrDefaultAsync(x => x.SupportCode == request.SupportCode);
        if (support is null)
        {
            throw new InvalidOperationException($"Support '{request.SupportCode}' was not found.");
        }

        var section = await _db.ProductionLineSections.FirstOrDefaultAsync(x => x.SectionCode == request.SectionCode);
        if (section is null)
        {
            throw new InvalidOperationException($"Section '{request.SectionCode}' was not found.");
        }

        support.CurrentSectionId = section.Id;
        support.Status = support.Status == "Available" ? "Loaded" : support.Status;

        var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.CurrentSupportId == support.Id);
        if (unit is not null)
        {
            unit.CurrentSectionId = section.Id;
        }

        _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
        {
            SupportId = support.Id,
            SectionId = section.Id,
            DateTime = DateTime.UtcNow,
            EventType = "ManualMovement"
        });

        await _db.SaveChangesAsync();
        return new { support.SupportCode, section.SectionCode, unit = unit?.UnitCode, message = "Support movement registered." };
    }

    private async Task<object> RegisterQualityAsync(ManualEventRequest request)
    {
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.UnitCode == request.ProductUnitCode);
        if (unit is null)
        {
            throw new InvalidOperationException($"Product unit '{request.ProductUnitCode}' was not found.");
        }

        var checkpoint = await _db.Checkpoints.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.SectionId == unit.CurrentSectionId)
            ?? await _db.Checkpoints.OrderBy(x => x.Id).FirstOrDefaultAsync();

        var result = string.IsNullOrWhiteSpace(request.Result) ? "PASS" : request.Result.Trim().ToUpperInvariant();
        unit.QualityStatus = result;
        unit.Status = result == "FAIL" ? "Blocked" : unit.Status;

        var quality = new QualityResult
        {
            ProductUnitId = unit.Id,
            CheckpointId = checkpoint?.Id,
            Result = result,
            RecordedAt = DateTime.UtcNow,
            Notes = request.Notes
        };
        _db.QualityResults.Add(quality);
        await _db.SaveChangesAsync();

        Nonconformity? nonconformity = null;
        if (result == "FAIL")
        {
            nonconformity = new Nonconformity
            {
                ProductUnitId = unit.Id,
                QualityResultId = quality.Id,
                Severity = "Major",
                Status = "Blocked",
                Description = request.Notes ?? "Manual quality failure registered."
            };
            _db.Nonconformities.Add(nonconformity);
            await _db.SaveChangesAsync();
        }

        return new { unit.UnitCode, result, nonconformityId = nonconformity?.Id, message = "Quality result registered." };
    }

    private async Task<object> TransferToRackAsync(ManualEventRequest request)
    {
        var support = await _db.Supports.FirstOrDefaultAsync(x => x.SupportCode == request.SupportCode);
        if (support is null)
        {
            throw new InvalidOperationException($"Support '{request.SupportCode}' was not found.");
        }

        var rack = await _db.Racks.FirstOrDefaultAsync(x => x.RackCode == request.SectionCode)
            ?? await _db.Racks.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (rack is null)
        {
            throw new InvalidOperationException("No rack exists in the system.");
        }

        _db.RackSupportAssignments.Add(new RackSupportAssignment
        {
            RackId = rack.Id,
            SupportId = support.Id,
            DateTimeIn = DateTime.UtcNow
        });

        support.Status = "Stored";
        if (rack.SectionId is not null)
        {
            support.CurrentSectionId = rack.SectionId;
            var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.CurrentSupportId == support.Id);
            if (unit is not null)
            {
                unit.CurrentSectionId = rack.SectionId;
                unit.Status = unit.QualityStatus == "FAIL" ? unit.Status : "Completed";
                unit.CompletedAt ??= DateTime.UtcNow;
            }

            _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
            {
                SupportId = support.Id,
                SectionId = rack.SectionId.Value,
                DateTime = DateTime.UtcNow,
                EventType = "TransferToRack"
            });
        }

        await _db.SaveChangesAsync();
        return new { support.SupportCode, rack.RackCode, message = "Support transferred to post-line rack." };
    }
}
