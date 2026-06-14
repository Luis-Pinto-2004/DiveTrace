using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public sealed class DemoEventService
{
    private readonly DriveTraceDbContext _db;
    private readonly OperationalEventService _operationalEvents;

    public DemoEventService(DriveTraceDbContext db, OperationalEventService operationalEvents)
    {
        _db = db;
        _operationalEvents = operationalEvents;
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

        throw new InvalidOperationException("Tipo de evento não suportado. Valores suportados: MoveSupport, RegisterQualityResult, TransferSupportToRack.");
    }

    public async Task<object> ExecutePlaybackAsync(PlaybackRequest request)
    {
        var supports = await _db.Supports.OrderBy(x => x.SupportCode).ToListAsync();
        var sections = await _db.ProductionLineSections.OrderBy(x => x.Id).ToListAsync();
        var events = new List<object>();

        foreach (var support in supports)
        {
            var now = DateTime.UtcNow;
            var currentIndex = support.CurrentSectionId is null
                ? -1
                : sections.FindIndex(x => x.Id == support.CurrentSectionId.Value);
            var nextIndex = Math.Min(currentIndex + 1, sections.Count - 1);
            var nextSection = sections[nextIndex];
            support.CurrentSectionId = nextSection.Id;

            var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.CurrentSupportId == support.Id);
            if (unit is not null)
            {
                var previousSection = unit.CurrentSectionId is null
                    ? null
                    : sections.FirstOrDefault(x => x.Id == unit.CurrentSectionId.Value);
                unit.CurrentSectionId = nextSection.Id;
                if (nextSection.SectionType.Contains("log", StringComparison.OrdinalIgnoreCase) || nextSection.Name.Contains("rack", StringComparison.OrdinalIgnoreCase))
                {
                    unit.Status = unit.QualityStatus == "FAIL" ? unit.Status : "Completed";
                    unit.CompletedAt ??= now;
                }

                _db.ProductUnitLocationHistory.Add(new ProductUnitLocationHistory
                {
                    ProductUnitId = unit.Id,
                    FromProductionLineId = previousSection?.LineId,
                    ToProductionLineId = nextSection.LineId,
                    FromSectionId = previousSection?.Id,
                    ToSectionId = nextSection.Id,
                    FromSupportId = support.Id,
                    ToSupportId = support.Id,
                    EventType = "PlaybackMovement",
                    Reason = "A reprodução demonstrativa moveu a unidade com o respetivo suporte.",
                    OccurredAt = now,
                    Source = "playback"
                });

                await _operationalEvents.RecordAsync(new OperationalEventCreateRequest
                {
                    EventCode = $"PLAYBACK-{request.Scenario}-{unit.UnitCode}-{support.SupportCode}-{now:yyyyMMddHHmmssfff}",
                    EventType = OperationalEventTypes.SectionMovement,
                    ProductUnitId = unit.Id,
                    SupportId = support.Id,
                    ManufacturingOrderId = unit.ManufacturingOrderId,
                    FromProductionLineId = previousSection?.LineId,
                    ToProductionLineId = nextSection.LineId,
                    FromSectionId = previousSection?.Id,
                    ToSectionId = nextSection.Id,
                    Source = OperationalEventSources.Playback,
                    OccurredAt = now,
                    Notes = "Reprodução demonstrativa moveu a unidade com o respetivo suporte.",
                    IsDemo = true,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["scenario"] = request.Scenario,
                        ["supportCode"] = support.SupportCode
                    }
                }, saveChanges: false);
            }

            _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
            {
                SupportId = support.Id,
                SectionId = nextSection.Id,
                DateTime = now,
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
            throw new InvalidOperationException($"Suporte '{request.SupportCode}' não encontrado.");
        }

        var section = await _db.ProductionLineSections.FirstOrDefaultAsync(x => x.SectionCode == request.SectionCode);
        if (section is null)
        {
            throw new InvalidOperationException($"Secção '{request.SectionCode}' não encontrada.");
        }

        support.CurrentSectionId = section.Id;
        support.Status = support.Status == "Available" ? "Loaded" : support.Status;
        var now = DateTime.UtcNow;

        var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.CurrentSupportId == support.Id);
        if (unit is not null)
        {
            var previousSection = unit.CurrentSectionId is null
                ? null
                : await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.CurrentSectionId.Value);
            unit.CurrentSectionId = section.Id;
            _db.ProductUnitLocationHistory.Add(new ProductUnitLocationHistory
            {
                ProductUnitId = unit.Id,
                FromProductionLineId = previousSection?.LineId,
                ToProductionLineId = section.LineId,
                FromSectionId = previousSection?.Id,
                ToSectionId = section.Id,
                FromSupportId = support.Id,
                ToSupportId = support.Id,
                EventType = "ManualMovement",
                Reason = request.Notes ?? "Evento manual moveu a unidade com o respetivo suporte.",
                Notes = request.Notes,
                OccurredAt = now,
                Source = "manual-event"
            });
        }

        _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
        {
            SupportId = support.Id,
            SectionId = section.Id,
            DateTime = now,
            EventType = "ManualMovement"
        });

        await _operationalEvents.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"MANUAL-MOVE-{support.SupportCode}-{section.SectionCode}-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SectionMovement,
            ProductUnitId = unit?.Id,
            SupportId = support.Id,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            ToProductionLineId = section.LineId,
            ToSectionId = section.Id,
            Source = OperationalEventSources.Manual,
            OccurredAt = now,
            Notes = request.Notes ?? "Evento manual moveu o suporte.",
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["supportCode"] = support.SupportCode,
                ["sectionCode"] = section.SectionCode
            }
        }, saveChanges: false);

        await _db.SaveChangesAsync();
        return new { support.SupportCode, section.SectionCode, unit = unit?.UnitCode, message = "Movimento de suporte registado." };
    }

    private async Task<object> RegisterQualityAsync(ManualEventRequest request)
    {
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.UnitCode == request.ProductUnitCode);
        if (unit is null)
        {
            throw new InvalidOperationException($"Unidade de produto '{request.ProductUnitCode}' não encontrada.");
        }

        var checkpoint = await _db.Checkpoints.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.SectionId == unit.CurrentSectionId)
            ?? await _db.Checkpoints.OrderBy(x => x.Id).FirstOrDefaultAsync();

        var result = string.IsNullOrWhiteSpace(request.Result) ? "PASS" : request.Result.Trim().ToUpperInvariant();
        unit.QualityStatus = result;
        unit.Status = result == "FAIL" ? "Blocked" : unit.Status;
        var now = DateTime.UtcNow;

        var quality = new QualityResult
        {
            ProductUnitId = unit.Id,
            CheckpointId = checkpoint?.Id,
            Result = result,
            RecordedAt = now,
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
                Severity = "Maior",
                Status = "Blocked",
                Description = request.Notes ?? "Falha de qualidade manual registada."
            };
            _db.Nonconformities.Add(nonconformity);
            await _db.SaveChangesAsync();
        }

        await _operationalEvents.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"QUALITY-{quality.Id}",
            EventType = OperationalEventTypes.QualityRecorded,
            ProductUnitId = unit.Id,
            ManufacturingOrderId = unit.ManufacturingOrderId,
            CheckpointId = checkpoint?.Id,
            QualityResultId = quality.Id,
            ReasonCode = result,
            Severity = result == "FAIL" ? "Maior" : null,
            Source = OperationalEventSources.Manual,
            OccurredAt = now,
            Notes = request.Notes ?? "Resultado de qualidade registado manualmente.",
            IsDemo = true
        }, saveChanges: false);

        if (nonconformity is not null)
        {
            await _operationalEvents.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"NC-{nonconformity.Id}",
                EventType = OperationalEventTypes.NonconformityOpened,
                ProductUnitId = unit.Id,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                CheckpointId = checkpoint?.Id,
                QualityResultId = quality.Id,
                NonconformityId = nonconformity.Id,
                Severity = nonconformity.Severity,
                Source = OperationalEventSources.Manual,
                OccurredAt = nonconformity.CreatedAt,
                Notes = nonconformity.Description,
                IsDemo = true
            }, saveChanges: false);
        }

        await _db.SaveChangesAsync();
        return new { unit.UnitCode, result, nonconformityId = nonconformity?.Id, message = "Resultado de qualidade registado." };
    }

    private async Task<object> TransferToRackAsync(ManualEventRequest request)
    {
        var support = await _db.Supports.FirstOrDefaultAsync(x => x.SupportCode == request.SupportCode);
        if (support is null)
        {
            throw new InvalidOperationException($"Suporte '{request.SupportCode}' não encontrado.");
        }

        var rack = await _db.Racks.FirstOrDefaultAsync(x => x.RackCode == request.SectionCode)
            ?? await _db.Racks.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (rack is null)
        {
            throw new InvalidOperationException("Não existe nenhuma rack no sistema.");
        }

        var now = DateTime.UtcNow;
        _db.RackSupportAssignments.Add(new RackSupportAssignment
        {
            RackId = rack.Id,
            SupportId = support.Id,
            DateTimeIn = now
        });

        support.Status = "Stored";
        ProductUnit? unit = null;
        if (rack.SectionId is not null)
        {
            support.CurrentSectionId = rack.SectionId;
            var rackSection = await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rack.SectionId.Value);
            unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.CurrentSupportId == support.Id);
            if (unit is not null)
            {
                var previousSection = unit.CurrentSectionId is null
                    ? null
                    : await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.CurrentSectionId.Value);
                unit.CurrentSectionId = rack.SectionId;
                unit.Status = unit.QualityStatus == "FAIL" ? unit.Status : "Completed";
                unit.CompletedAt ??= now;
                _db.ProductUnitLocationHistory.Add(new ProductUnitLocationHistory
                {
                    ProductUnitId = unit.Id,
                    FromProductionLineId = previousSection?.LineId,
                    ToProductionLineId = rackSection?.LineId,
                    FromSectionId = previousSection?.Id,
                    ToSectionId = rack.SectionId.Value,
                    FromSupportId = support.Id,
                    ToSupportId = support.Id,
                    EventType = "TransferToRack",
                    Reason = request.Notes ?? "Suporte transferido para rack pós-linha.",
                    Notes = request.Notes,
                    OccurredAt = now,
                    Source = "manual-event"
                });
            }

            _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
            {
                SupportId = support.Id,
                SectionId = rack.SectionId.Value,
                DateTime = now,
                EventType = "TransferToRack"
            });
        }

        await _operationalEvents.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"RACK-ASSIGNED-{rack.RackCode}-{support.SupportCode}-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.RackAssigned,
            ProductUnitId = unit?.Id,
            SupportId = support.Id,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            ToSectionId = rack.SectionId,
            RackId = rack.Id,
            Source = OperationalEventSources.Manual,
            OccurredAt = now,
            Notes = request.Notes ?? "Suporte transferido para rack pós-linha.",
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["rackCode"] = rack.RackCode,
                ["supportCode"] = support.SupportCode
            }
        }, saveChanges: false);

        await _db.SaveChangesAsync();
        return new { support.SupportCode, rack.RackCode, message = "Suporte transferido para rack pós-linha." };
    }
}
