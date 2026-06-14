using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public sealed class ProductUnitTransferRequest
{
    public int ToSectionId { get; set; }
    public int? ToSupportId { get; set; }
    public string? EventType { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? OperatorUserId { get; set; }
    public bool MoveCurrentSupport { get; set; } = true;
}

public sealed class ProductionTimelineItem
{
    public string EventType { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? LineCode { get; init; }
    public string? SectionCode { get; init; }
    public string? SupportCode { get; init; }
    public string? Result { get; init; }
}

public sealed class ProductionFlowService
{
    private readonly DriveTraceDbContext _db;
    private readonly OperationalEventService _operationalEvents;

    public ProductionFlowService(DriveTraceDbContext db, OperationalEventService operationalEvents)
    {
        _db = db;
        _operationalEvents = operationalEvents;
    }

    public async Task<object> TransferAsync(int productUnitId, ProductUnitTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ToSectionId <= 0)
        {
            throw new InvalidOperationException("É obrigatória uma secção de destino.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.Id == productUnitId, cancellationToken);
        if (unit is null)
        {
            throw new KeyNotFoundException($"Unidade de produto com id {productUnitId} não encontrada.");
        }

        if (IsClosedUnit(unit))
        {
            throw new InvalidOperationException("Unidades concluídas ou em sucata não podem ser transferidas.");
        }

        var toSection = await _db.ProductionLineSections.FirstOrDefaultAsync(x => x.Id == request.ToSectionId, cancellationToken);
        if (toSection is null)
        {
            throw new InvalidOperationException($"Secção de destino com id {request.ToSectionId} não encontrada.");
        }

        var fromSection = unit.CurrentSectionId.HasValue
            ? await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.CurrentSectionId.Value, cancellationToken)
            : null;

        var fromSupportId = unit.CurrentSupportId;
        Support? toSupport = null;
        if (request.ToSupportId.HasValue)
        {
            toSupport = await _db.Supports.FirstOrDefaultAsync(x => x.Id == request.ToSupportId.Value, cancellationToken);
            if (toSupport is null)
            {
                throw new InvalidOperationException($"Suporte de destino com id {request.ToSupportId.Value} não encontrado.");
            }
        }
        else if (request.MoveCurrentSupport && unit.CurrentSupportId.HasValue)
        {
            toSupport = await _db.Supports.FirstOrDefaultAsync(x => x.Id == unit.CurrentSupportId.Value, cancellationToken);
        }

        if (toSupport is not null && toSupport.Id != fromSupportId)
        {
            var openAssignments = await _db.UnitSupportAssignments
                .Where(x => x.ProductUnitId == unit.Id && x.DateTimeOut == null)
                .ToListAsync(cancellationToken);
            foreach (var assignment in openAssignments)
            {
                assignment.DateTimeOut = now;
            }

            _db.UnitSupportAssignments.Add(new UnitSupportAssignment
            {
                ProductUnitId = unit.Id,
                SupportId = toSupport.Id,
                DateTimeIn = now
            });

            unit.CurrentSupportId = toSupport.Id;
        }

        unit.CurrentSectionId = toSection.Id;

        if (IsPostLineSection(toSection) && !unit.QualityStatus.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            unit.Status = "Completed";
            unit.CompletedAt ??= now;
        }
        else if (!IsAttentionStatus(unit.Status))
        {
            unit.Status = "Active";
            unit.CompletedAt = null;
        }

        if (toSupport is not null && (request.MoveCurrentSupport || toSupport.Id != fromSupportId))
        {
            toSupport.CurrentSectionId = toSection.Id;
            if (toSupport.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
            {
                toSupport.Status = "Loaded";
            }

            _db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
            {
                SupportId = toSupport.Id,
                SectionId = toSection.Id,
                DateTime = now,
                EventType = "UnitTransfer"
            });
        }

        var movement = new ProductUnitLocationHistory
        {
            ProductUnitId = unit.Id,
            FromProductionLineId = fromSection?.LineId,
            ToProductionLineId = toSection.LineId,
            FromSectionId = fromSection?.Id,
            ToSectionId = toSection.Id,
            FromSupportId = fromSupportId,
            ToSupportId = toSupport?.Id ?? unit.CurrentSupportId,
            EventType = CleanValue(request.EventType, "LineTransfer"),
            Reason = CleanValue(request.Reason, "Transferência operacional"),
            Notes = request.Notes,
            OperatorUserId = request.OperatorUserId,
            OccurredAt = now,
            Source = "api",
            CorrelationId = Guid.NewGuid().ToString("N")
        };
        _db.ProductUnitLocationHistory.Add(movement);

        await _operationalEvents.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"TRANSFER-{movement.CorrelationId}",
            EventType = fromSection?.LineId != toSection.LineId ? OperationalEventTypes.LineTransfer : OperationalEventTypes.SectionMovement,
            ProductUnitId = unit.Id,
            SupportId = toSupport?.Id ?? unit.CurrentSupportId,
            ManufacturingOrderId = unit.ManufacturingOrderId,
            FromProductionLineId = fromSection?.LineId,
            ToProductionLineId = toSection.LineId,
            FromSectionId = fromSection?.Id,
            ToSectionId = toSection.Id,
            ReasonCode = movement.EventType,
            Source = OperationalEventSources.Api,
            PerformedByUserId = request.OperatorUserId,
            OccurredAt = now,
            Notes = movement.Notes ?? movement.Reason,
            Metadata = new Dictionary<string, object?>
            {
                ["movementCorrelationId"] = movement.CorrelationId,
                ["moveCurrentSupport"] = request.MoveCurrentSupport,
                ["fromSupportId"] = fromSupportId,
                ["toSupportId"] = toSupport?.Id ?? unit.CurrentSupportId
            }
        }, saveChanges: false, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildTransferResponseAsync(unit.Id, movement.Id, cancellationToken);
    }

    public async Task<object?> GetTraceAsync(int productUnitId, CancellationToken cancellationToken = default)
    {
        var unit = await _db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productUnitId, cancellationToken);
        if (unit is null) return null;

        var order = await _db.ManufacturingOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.ManufacturingOrderId, cancellationToken);
        var customer = order?.CustomerId is null
            ? null
            : await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.CustomerId.Value, cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().ToListAsync(cancellationToken);
        var lines = await _db.ProductionLines.AsNoTracking().ToListAsync(cancellationToken);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);
        var supportById = supports.ToDictionary(x => x.Id);
        var currentSection = unit.CurrentSectionId.HasValue && sectionById.TryGetValue(unit.CurrentSectionId.Value, out var sectionValue)
            ? sectionValue
            : null;
        var currentLine = currentSection?.LineId is not null && lineById.TryGetValue(currentSection.LineId.Value, out var lineValue)
            ? lineValue
            : null;

        var locationHistory = await _db.ProductUnitLocationHistory
            .AsNoTracking()
            .Where(x => x.ProductUnitId == productUnitId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(cancellationToken);

        var assignments = await _db.UnitSupportAssignments
            .AsNoTracking()
            .Where(x => x.ProductUnitId == productUnitId)
            .OrderBy(x => x.DateTimeIn)
            .ToListAsync(cancellationToken);

        var supportIds = assignments
            .Select(x => x.SupportId)
            .Concat(unit.CurrentSupportId.HasValue ? new[] { unit.CurrentSupportId.Value } : Array.Empty<int>())
            .Distinct()
            .ToArray();

        var supportMovements = await _db.SupportLocalizationHistory
            .AsNoTracking()
            .Where(x => supportIds.Contains(x.SupportId))
            .OrderBy(x => x.DateTime)
            .ToListAsync(cancellationToken);

        var materialUsage = await _db.UnitMaterialLotUsages
            .AsNoTracking()
            .Where(x => x.ProductUnitId == productUnitId)
            .Join(_db.LotRawMaterials, usage => usage.LotId, lot => lot.Id, (usage, lot) => new { usage, lot })
            .Join(_db.RawMaterials, ul => ul.lot.RawMaterialId, mat => mat.Id, (ul, mat) => new
            {
                rawMaterial = mat.Name,
                lotNumber = ul.lot.LotNumber,
                ul.usage.AssociationType,
                ul.usage.Quantity,
                unit = ul.lot.LotUnit
            })
            .ToListAsync(cancellationToken);

        var quality = await _db.QualityResults.AsNoTracking().Where(x => x.ProductUnitId == productUnitId).OrderBy(x => x.RecordedAt).ToListAsync(cancellationToken);
        var nonconformities = await _db.Nonconformities.AsNoTracking().Where(x => x.ProductUnitId == productUnitId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var rework = await _db.ReworkRecords.AsNoTracking().Where(x => x.ProductUnitId == productUnitId).OrderBy(x => x.StartedAt).ToListAsync(cancellationToken);
        var scrap = await _db.ScrapRecords.AsNoTracking().Where(x => x.ProductUnitId == productUnitId).OrderBy(x => x.ScrappedAt).ToListAsync(cancellationToken);
        var operationalEvents = await _operationalEvents.GetForProductUnitAsync(productUnitId, cancellationToken: cancellationToken);

        var timeline = BuildTimeline(locationHistory, supportMovements, quality, operationalEvents, sectionById, lineById, supportById);

        return new
        {
            generatedAt = DateTime.UtcNow,
            unit = new
            {
                unit.Id,
                unit.UnitCode,
                unit.UnitType,
                unit.Status,
                unit.QualityStatus,
                unit.CreatedAt,
                unit.CompletedAt,
                currentSupport = Ref(unit.CurrentSupportId, unit.CurrentSupportId.HasValue && supportById.TryGetValue(unit.CurrentSupportId.Value, out var currentSupport) ? currentSupport.SupportCode : null, null),
                currentSection = SectionRef(currentSection),
                currentProductionLine = LineRef(currentLine),
                lastMovementAt = LastMovementAt(locationHistory),
                routeState = RouteState(unit, currentSection)
            },
            order = order is null ? null : new
            {
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PlannedQty,
                order.ScheduledUntil,
                order.PublicTrackingCode,
                order.CustomerReference
            },
            customer = customer is null ? null : new { customer.Id, customer.CustomerCode, customer.Name, customer.ContactEmail },
            locationHistory = locationHistory.Select(x => new
            {
                x.Id,
                x.EventType,
                x.Reason,
                x.Notes,
                x.OperatorUserId,
                x.OccurredAt,
                x.Source,
                fromProductionLine = LineRef(Find(lineById, x.FromProductionLineId)),
                toProductionLine = LineRef(Find(lineById, x.ToProductionLineId)),
                fromSection = SectionRef(Find(sectionById, x.FromSectionId)),
                toSection = SectionRef(Find(sectionById, x.ToSectionId)),
                fromSupport = SupportRef(Find(supportById, x.FromSupportId)),
                toSupport = SupportRef(Find(supportById, x.ToSupportId))
            }),
            supportAssignments = assignments.Select(x => new
            {
                x.Id,
                support = SupportRef(Find(supportById, x.SupportId)),
                x.DateTimeIn,
                x.DateTimeOut
            }),
            supportMovements = supportMovements.Select(x => new
            {
                x.Id,
                support = SupportRef(Find(supportById, x.SupportId)),
                section = SectionRef(Find(sectionById, x.SectionId)),
                x.EventType,
                x.DateTime
            }),
            materialUsage,
            quality,
            nonconformities,
            rework,
            scrap,
            operationalEvents,
            timeline
        };
    }

    public async Task<object> GetFlowSummaryAsync(CancellationToken cancellationToken = default)
    {
        var lines = await _db.ProductionLines.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.LineCode).ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.SectionCode).ToListAsync(cancellationToken);
        var units = await _db.ProductUnits.AsNoTracking().ToListAsync(cancellationToken);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var histories = await _db.ProductUnitLocationHistory.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(50).ToListAsync(cancellationToken);
        var recentOperationalEvents = await _operationalEvents.GetRecentAsync(12, cancellationToken);

        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);
        var supportById = supports.ToDictionary(x => x.Id);
        var activeUnits = units.Where(IsFlowUnit).ToList();
        var lastMovementByUnit = histories
            .GroupBy(x => x.ProductUnitId)
            .ToDictionary(x => x.Key, x => x.Max(item => item.OccurredAt));

        var lineSummaries = lines.Select(line =>
        {
            var lineSections = sections.Where(section => section.LineId == line.Id).OrderBy(section => section.DisplayOrder).ThenBy(section => section.SectionCode).ToList();
            var lineSectionIds = lineSections.Select(section => section.Id).ToHashSet();
            var lineUnits = activeUnits.Where(unit => unit.CurrentSectionId.HasValue && lineSectionIds.Contains(unit.CurrentSectionId.Value)).ToList();
            return new
            {
                productionLineId = line.Id,
                line.LineCode,
                line.Name,
                line.DisplayOrder,
                line.VisualGroup,
                wipUnits = lineUnits.Count,
                activeSupports = supports.Count(support => support.CurrentSectionId.HasValue && lineSectionIds.Contains(support.CurrentSectionId.Value)),
                blockedUnits = lineUnits.Count(IsAttentionUnit),
                lastMovementAt = lineUnits
                    .Select(unit => lastMovementByUnit.TryGetValue(unit.Id, out var value) ? value : (DateTime?)null)
                    .Where(value => value.HasValue)
                    .Max(),
                sections = lineSections.Select(section =>
                {
                    var sectionUnits = activeUnits.Where(unit => unit.CurrentSectionId == section.Id).ToList();
                    return new
                    {
                        sectionId = section.Id,
                        section.SectionCode,
                        section.Name,
                        section.SectionType,
                        section.DisplayOrder,
                        section.LayoutColumn,
                        section.LayoutRow,
                        section.VisualZone,
                        section.IsTransferPoint,
                        section.AllowsLineTransferIn,
                        section.AllowsLineTransferOut,
                        wipUnits = sectionUnits.Count,
                        activeSupports = supports.Count(support => support.CurrentSectionId == section.Id),
                        blockedUnits = sectionUnits.Count(IsAttentionUnit)
                    };
                })
            };
        }).ToList();

        var recentTransfers = histories
            .Take(12)
            .Select(x => new
            {
                x.Id,
                unit = UnitRef(units.FirstOrDefault(unit => unit.Id == x.ProductUnitId)),
                x.EventType,
                x.Reason,
                x.OccurredAt,
                fromProductionLine = LineRef(Find(lineById, x.FromProductionLineId)),
                toProductionLine = LineRef(Find(lineById, x.ToProductionLineId)),
                fromSection = SectionRef(Find(sectionById, x.FromSectionId)),
                toSection = SectionRef(Find(sectionById, x.ToSectionId)),
                toSupport = SupportRef(Find(supportById, x.ToSupportId))
            })
            .ToList();

        return new
        {
            generatedAt = DateTime.UtcNow,
            totals = new
            {
                productionLines = lines.Count,
                sections = sections.Count,
                activeUnits = activeUnits.Count,
                activeSupports = supports.Count(support => !support.Status.Equals("Available", StringComparison.OrdinalIgnoreCase)),
                transferPoints = sections.Count(section => section.IsTransferPoint),
                transfers = await _db.ProductUnitLocationHistory.CountAsync(cancellationToken),
                transfersLast24h = await _db.ProductUnitLocationHistory.CountAsync(x => x.OccurredAt >= DateTime.UtcNow.AddHours(-24), cancellationToken),
                operationalEvents = await _db.OperationalEvents.CountAsync(cancellationToken),
                operationalEventsLast24h = await _db.OperationalEvents.CountAsync(x => x.OccurredAt >= DateTime.UtcNow.AddHours(-24), cancellationToken)
            },
            routeStates = activeUnits
                .Select(unit => new { state = RouteState(unit, unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value) : null) })
                .GroupBy(x => x.state)
                .Select(group => new { routeState = group.Key, count = group.Count() })
                .OrderByDescending(x => x.count),
            lineSummaries,
            recentTransfers,
            recentOperationalEvents
        };
    }

    public async Task<object> GetOperatorWorkbenchAsync(CancellationToken cancellationToken = default)
    {
        var lines = await _db.ProductionLines.AsNoTracking().ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var units = await _db.ProductUnits.AsNoTracking().OrderBy(x => x.UnitCode).ToListAsync(cancellationToken);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var histories = await _db.ProductUnitLocationHistory.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(20).ToListAsync(cancellationToken);
        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);
        var supportById = supports.ToDictionary(x => x.Id);

        var activeUnits = units.Where(IsFlowUnit).ToList();
        var transferReady = activeUnits
            .Where(unit => unit.CurrentSectionId.HasValue && Find(sectionById, unit.CurrentSectionId.Value)?.AllowsLineTransferOut == true)
            .ToList();
        var blocked = activeUnits.Where(IsAttentionUnit).ToList();
        var rework = activeUnits.Where(unit => unit.Status.Equals("Rework", StringComparison.OrdinalIgnoreCase)).ToList();
        var noSupport = activeUnits.Where(unit => unit.CurrentSupportId is null).ToList();

        return new
        {
            generatedAt = DateTime.UtcNow,
            queues = new
            {
                transferReady = transferReady.Count,
                blocked = blocked.Count,
                rework = rework.Count,
                noSupport = noSupport.Count
            },
            transferTargets = sections
                .Where(section => section.AllowsLineTransferIn)
                .Select(section => new
                {
                    sectionId = section.Id,
                    section.SectionCode,
                    section.Name,
                    section.SectionType,
                    productionLine = LineRef(section.LineId.HasValue ? Find(lineById, section.LineId.Value) : null),
                    currentWip = activeUnits.Count(unit => unit.CurrentSectionId == section.Id)
                }),
            units = activeUnits.Select(unit =>
            {
                var section = unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value) : null;
                return new
                {
                    unit.Id,
                    unit.UnitCode,
                    unit.Status,
                    unit.QualityStatus,
                    currentSection = SectionRef(section),
                    currentProductionLine = LineRef(section?.LineId is null ? null : Find(lineById, section.LineId.Value)),
                    currentSupport = SupportRef(unit.CurrentSupportId.HasValue ? Find(supportById, unit.CurrentSupportId.Value) : null),
                    routeState = RouteState(unit, section),
                    canTransfer = section?.AllowsLineTransferOut == true && !IsClosedUnit(unit),
                    requiresAttention = IsAttentionUnit(unit)
                };
            }),
            recentTransfers = histories.Take(10).Select(x => new
            {
                unit = UnitRef(units.FirstOrDefault(unit => unit.Id == x.ProductUnitId)),
                x.EventType,
                x.Reason,
                x.OccurredAt,
                fromSection = SectionRef(Find(sectionById, x.FromSectionId)),
                toSection = SectionRef(Find(sectionById, x.ToSectionId))
            })
        };
    }

    public async Task<object?> GetCustomerOrderAsync(string publicTrackingCode, CancellationToken cancellationToken = default)
    {
        var code = publicTrackingCode.Trim();
        var order = await _db.ManufacturingOrders.AsNoTracking().FirstOrDefaultAsync(x => x.PublicTrackingCode == code, cancellationToken);
        if (order is null) return null;

        var customer = order.CustomerId.HasValue
            ? await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.CustomerId.Value, cancellationToken)
            : null;
        var units = await _db.ProductUnits.AsNoTracking().Where(x => x.ManufacturingOrderId == order.Id).OrderBy(x => x.UnitCode).ToListAsync(cancellationToken);
        var unitIds = units.Select(x => x.Id).ToArray();
        var sections = await _db.ProductionLineSections.AsNoTracking().ToListAsync(cancellationToken);
        var lines = await _db.ProductionLines.AsNoTracking().ToListAsync(cancellationToken);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var histories = await _db.ProductUnitLocationHistory.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).OrderBy(x => x.OccurredAt).ToListAsync(cancellationToken);
        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);
        var supportById = supports.ToDictionary(x => x.Id);
        var historyByUnit = histories.GroupBy(x => x.ProductUnitId).ToDictionary(x => x.Key, x => x.ToList());

        return new
        {
            generatedAt = DateTime.UtcNow,
            publicTrackingCode = order.PublicTrackingCode,
            customerReference = order.CustomerReference,
            customer = customer is null ? null : new { customer.CustomerCode, customer.Name },
            order = new
            {
                order.OrderNumber,
                order.Status,
                order.PlannedQty,
                order.ScheduledUntil
            },
            summary = new
            {
                units = units.Count,
                completed = units.Count(unit => unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)),
                inFlow = units.Count(IsFlowUnit),
                attention = units.Count(IsAttentionUnit),
                lastMovementAt = histories.Count == 0 ? (DateTime?)null : histories.Max(x => x.OccurredAt)
            },
            units = units.Select(unit =>
            {
                var section = unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value) : null;
                var line = section?.LineId is null ? null : Find(lineById, section.LineId.Value);
                var unitHistory = historyByUnit.TryGetValue(unit.Id, out var value) ? value : new List<ProductUnitLocationHistory>();
                return new
                {
                    unit.UnitCode,
                    unit.Status,
                    unit.QualityStatus,
                    currentProductionLine = LineRef(line),
                    currentSection = SectionRef(section),
                    currentSupport = SupportRef(unit.CurrentSupportId.HasValue ? Find(supportById, unit.CurrentSupportId.Value) : null),
                    routeState = RouteState(unit, section),
                    lastMovementAt = LastMovementAt(unitHistory)
                };
            }),
            milestones = histories.Select(x => new
            {
                unit = UnitRef(units.FirstOrDefault(unit => unit.Id == x.ProductUnitId)),
                x.EventType,
                x.Reason,
                x.OccurredAt,
                fromProductionLine = LineRef(Find(lineById, x.FromProductionLineId)),
                toProductionLine = LineRef(Find(lineById, x.ToProductionLineId)),
                toSection = SectionRef(Find(sectionById, x.ToSectionId))
            })
        };
    }

    private async Task<object> BuildTransferResponseAsync(int unitId, int movementId, CancellationToken cancellationToken)
    {
        var trace = await GetTraceAsync(unitId, cancellationToken);
        return new
        {
            transferred = true,
            movementId,
            trace
        };
    }

    private static List<ProductionTimelineItem> BuildTimeline(
        IReadOnlyList<ProductUnitLocationHistory> locationHistory,
        IReadOnlyList<SupportLocalizationHistory> supportMovements,
        IReadOnlyList<QualityResult> quality,
        IReadOnlyList<OperationalEventDto> operationalEvents,
        IReadOnlyDictionary<int, ProductionLineSection> sectionById,
        IReadOnlyDictionary<int, ProductionLine> lineById,
        IReadOnlyDictionary<int, Support> supportById)
    {
        var timeline = new List<ProductionTimelineItem>();

        timeline.AddRange(locationHistory.Select(item =>
        {
            var toSection = Find(sectionById, item.ToSectionId);
            var toLine = toSection?.LineId is null ? null : Find(lineById, toSection.LineId.Value);
            return new ProductionTimelineItem
            {
                EventType = item.EventType,
                OccurredAt = item.OccurredAt,
                Source = item.Source,
                Label = item.Reason,
                LineCode = toLine?.LineCode,
                SectionCode = toSection?.SectionCode,
                SupportCode = Find(supportById, item.ToSupportId)?.SupportCode
            };
        }));

        timeline.AddRange(supportMovements.Select(item =>
        {
            var section = Find(sectionById, item.SectionId);
            var line = section?.LineId is null ? null : Find(lineById, section.LineId.Value);
            return new ProductionTimelineItem
            {
                EventType = item.EventType,
                OccurredAt = item.DateTime,
                Source = "support",
                Label = "Movimento de suporte",
                LineCode = line?.LineCode,
                SectionCode = section?.SectionCode,
                SupportCode = Find(supportById, item.SupportId)?.SupportCode
            };
        }));

        timeline.AddRange(quality.Select(item => new ProductionTimelineItem
        {
            EventType = "QualityResult",
            OccurredAt = item.RecordedAt,
            Source = "quality",
            Label = item.Notes ?? "Resultado de qualidade",
            Result = item.Result
        }));

        timeline.AddRange(operationalEvents.Select(item => new ProductionTimelineItem
        {
            EventType = item.EventType,
            OccurredAt = item.OccurredAt,
            Source = item.Source,
            Label = item.Label,
            Result = item.Severity
        }));

        return timeline.OrderBy(item => item.OccurredAt).ToList();
    }

    private static bool IsFlowUnit(ProductUnit unit)
    {
        return !IsClosedUnit(unit);
    }

    private static bool IsClosedUnit(ProductUnit unit)
    {
        return unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            || unit.Status.Equals("Scrap", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAttentionUnit(ProductUnit unit)
    {
        return unit.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || unit.Status.Equals("Rework", StringComparison.OrdinalIgnoreCase)
            || unit.QualityStatus.Equals("FAIL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAttentionStatus(string status)
    {
        return status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Rework", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Scrap", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPostLineSection(ProductionLineSection section)
    {
        var value = $"{section.SectionType} {section.Name}".ToLowerInvariant();
        return value.Contains("post-line") || value.Contains("log") || value.Contains("rack") || value.Contains("expedition");
    }

    private static string RouteState(ProductUnit unit, ProductionLineSection? section)
    {
        if (unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Completed";
        if (unit.Status.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) return "Scrap";
        if (IsAttentionUnit(unit)) return "Attention";
        if (section is null) return "Unassigned";
        if (IsPostLineSection(section)) return "PostLine";
        if (section.IsTransferPoint) return "TransferPoint";
        return section.LineId.HasValue ? "InLine" : "Unassigned";
    }

    private static DateTime? LastMovementAt(IReadOnlyList<ProductUnitLocationHistory> history)
    {
        return history.Count == 0 ? null : history.Max(x => x.OccurredAt);
    }

    private static string CleanValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static T? Find<T>(IReadOnlyDictionary<int, T> items, int? id)
    {
        return id.HasValue && items.TryGetValue(id.Value, out var value) ? value : default;
    }

    private static object? Ref(int? id, string? code, string? name)
    {
        return id.HasValue ? new { id, code, name } : null;
    }

    private static object? UnitRef(ProductUnit? unit)
    {
        return unit is null ? null : new { id = unit.Id, code = unit.UnitCode, status = unit.Status };
    }

    private static object? SupportRef(Support? support)
    {
        return support is null ? null : new { id = support.Id, code = support.SupportCode, status = support.Status };
    }

    private static object? SectionRef(ProductionLineSection? section)
    {
        return section is null
            ? null
            : new
            {
                id = section.Id,
                code = section.SectionCode,
                name = section.Name,
                type = section.SectionType,
                lineId = section.LineId,
                isTransferPoint = section.IsTransferPoint,
                allowsLineTransferIn = section.AllowsLineTransferIn,
                allowsLineTransferOut = section.AllowsLineTransferOut
            };
    }

    private static object? LineRef(ProductionLine? line)
    {
        return line is null
            ? null
            : new
            {
                id = line.Id,
                code = line.LineCode,
                name = line.Name,
                visualGroup = line.VisualGroup,
                displayOrder = line.DisplayOrder
            };
    }
}
