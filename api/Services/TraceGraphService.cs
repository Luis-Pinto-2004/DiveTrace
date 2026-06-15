using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public sealed record TraceGraphAccessContext(
    string Role,
    string RoleKey,
    string Username,
    string? LineCode,
    string? SectionCode,
    string? CustomerCode);

public sealed class TraceGraphService
{
    private readonly DriveTraceDbContext _db;
    private readonly IFiwareContextService _fiware;

    public TraceGraphService(DriveTraceDbContext db, IFiwareContextService fiware)
    {
        _db = db;
        _fiware = fiware;
    }

    public async Task<TraceGraphDto> GetFactoryGraphAsync(TraceGraphAccessContext access, CancellationToken cancellationToken = default)
    {
        var lines = await _db.ProductionLines.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.LineCode).ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.SectionCode).ToListAsync(cancellationToken);
        var units = await _db.ProductUnits.AsNoTracking().ToListAsync(cancellationToken);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var racks = await _db.Racks.AsNoTracking().ToListAsync(cancellationToken);
        var rackAssignments = await _db.RackSupportAssignments.AsNoTracking().Where(x => x.DateTimeOut == null).ToListAsync(cancellationToken);
        var histories = await _db.ProductUnitLocationHistory.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(60).ToListAsync(cancellationToken);
        var reconditioning = await _db.ReconditionRecords.AsNoTracking().ToListAsync(cancellationToken);

        var allowedSectionIds = AllowedSectionIds(access, lines, sections);
        var visibleSections = sections.Where(section => allowedSectionIds.Contains(section.Id)).ToList();
        var visibleLineIds = visibleSections.Select(section => section.LineId).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var visibleLines = lines.Where(line => visibleLineIds.Contains(line.Id) || !IsOperator(access)).ToList();
        var visibleUnits = units.Where(unit => !IsClosed(unit) && unit.CurrentSectionId.HasValue && allowedSectionIds.Contains(unit.CurrentSectionId.Value)).ToList();

        var nodes = new List<TraceGraphNodeDto>();
        var edges = new List<TraceGraphEdgeDto>();
        var warnings = new List<TraceGraphWarningDto>();
        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);

        foreach (var line in visibleLines.OrderBy(x => x.DisplayOrder).ThenBy(x => x.LineCode))
        {
            var lineSectionIds = visibleSections.Where(section => section.LineId == line.Id).Select(section => section.Id).ToHashSet();
            var lineUnits = visibleUnits.Where(unit => unit.CurrentSectionId.HasValue && lineSectionIds.Contains(unit.CurrentSectionId.Value)).ToList();
            nodes.Add(Node(
                $"line:{line.LineCode}",
                "ProductionLine",
                line.Name,
                line.LineCode,
                lineUnits.Count > 0 ? "Active" : "Available",
                lineUnits.Any(IsAttention) ? "warning" : "normal",
                line.LineCode,
                new Dictionary<string, object?>
                {
                    ["lineId"] = line.Id,
                    ["displayOrder"] = line.DisplayOrder,
                    ["wipUnits"] = lineUnits.Count,
                    ["activeSupports"] = supports.Count(support => support.CurrentSectionId.HasValue && lineSectionIds.Contains(support.CurrentSectionId.Value))
                }));
        }

        foreach (var section in visibleSections)
        {
            var sectionUnits = visibleUnits.Where(unit => unit.CurrentSectionId == section.Id).ToList();
            var blocked = sectionUnits.Count(IsAttention);
            var severity = blocked > 0 ? "warning" : sectionUnits.Count > 2 ? "attention" : "normal";
            nodes.Add(Node(
                SectionNodeId(section),
                "Section",
                section.Name,
                $"{section.SectionCode} · {section.SectionType}",
                sectionUnits.Count > 0 ? "Active" : "Available",
                severity,
                section.LineId.HasValue && lineById.TryGetValue(section.LineId.Value, out var line) ? line.LineCode : null,
                new Dictionary<string, object?>
                {
                    ["sectionId"] = section.Id,
                    ["sectionCode"] = section.SectionCode,
                    ["lineId"] = section.LineId,
                    ["layoutColumn"] = section.LayoutColumn,
                    ["layoutRow"] = section.LayoutRow,
                    ["wipUnits"] = sectionUnits.Count,
                    ["blockedUnits"] = blocked,
                    ["activeSupports"] = supports.Count(support => support.CurrentSectionId == section.Id),
                    ["isTransferPoint"] = section.IsTransferPoint,
                    ["allowsLineTransferIn"] = section.AllowsLineTransferIn,
                    ["allowsLineTransferOut"] = section.AllowsLineTransferOut
                }));

            if (section.LineId.HasValue && lineById.TryGetValue(section.LineId.Value, out var sectionLine))
            {
                edges.Add(Edge(
                    $"edge-line-section:{sectionLine.LineCode}:{section.SectionCode}",
                    $"line:{sectionLine.LineCode}",
                    SectionNodeId(section),
                    "contains",
                    "contém",
                    null,
                    "normal"));
            }
        }

        foreach (var lineGroup in visibleSections.Where(x => x.LineId.HasValue).GroupBy(x => x.LineId!.Value))
        {
            var ordered = lineGroup.OrderBy(x => x.DisplayOrder).ThenBy(x => x.SectionCode).ToList();
            for (var i = 0; i < ordered.Count - 1; i++)
            {
                edges.Add(Edge(
                    $"edge-route:{ordered[i].SectionCode}:{ordered[i + 1].SectionCode}",
                    SectionNodeId(ordered[i]),
                    SectionNodeId(ordered[i + 1]),
                    "route",
                    "fluxo nominal",
                    null,
                    "normal"));
            }
        }

        var transferCandidates = visibleSections.Where(x => x.AllowsLineTransferOut).ToList();
        foreach (var source in transferCandidates)
        {
            var targets = visibleSections
                .Where(target => target.AllowsLineTransferIn && target.Id != source.Id && target.LineId != source.LineId)
                .OrderBy(target => target.DisplayOrder)
                .Take(3);
            foreach (var target in targets)
            {
                edges.Add(Edge(
                    $"edge-transfer:{source.SectionCode}:{target.SectionCode}",
                    SectionNodeId(source),
                    SectionNodeId(target),
                    "transfer",
                    "transferência possível",
                    null,
                    "attention"));
            }
        }

        foreach (var unit in visibleUnits.OrderBy(x => x.UnitCode))
        {
            var severity = IsAttention(unit) ? "warning" : "normal";
            nodes.Add(Node(
                UnitNodeId(unit),
                "ProductUnit",
                unit.UnitCode,
                UnitSubtitle(unit),
                unit.Status,
                severity,
                unit.CurrentSectionId.HasValue && sectionById.TryGetValue(unit.CurrentSectionId.Value, out var section)
                    ? section.SectionCode
                    : null,
                new Dictionary<string, object?>
                {
                    ["unitId"] = unit.Id,
                    ["qualityStatus"] = unit.QualityStatus,
                    ["isReconditioned"] = unit.IsReconditioned,
                    ["recoveryStatus"] = unit.RecoveryStatus,
                    ["qualityDisposition"] = unit.QualityDisposition,
                    ["currentSectionId"] = unit.CurrentSectionId,
                    ["currentSupportId"] = unit.CurrentSupportId,
                    ["routeState"] = RouteState(unit, unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value) : null)
                }));

            if (unit.CurrentSectionId.HasValue && sectionById.TryGetValue(unit.CurrentSectionId.Value, out var unitSection))
            {
                edges.Add(Edge(
                    $"edge-section-unit:{unitSection.SectionCode}:{unit.UnitCode}",
                    SectionNodeId(unitSection),
                    UnitNodeId(unit),
                    "current-location",
                    "está em",
                    null,
                    severity));
            }
        }

        if (CanSeeLogistics(access))
        {
            foreach (var rack in racks.Where(rack => !rack.SectionId.HasValue || allowedSectionIds.Contains(rack.SectionId.Value)).OrderBy(x => x.RackCode))
            {
                var activeAssignments = rackAssignments.Count(assignment => assignment.RackId == rack.Id);
                nodes.Add(Node(
                    RackNodeId(rack),
                    "Rack",
                    rack.RackCode,
                    rack.SectionId.HasValue && sectionById.TryGetValue(rack.SectionId.Value, out var rackSection) ? rackSection.Name : "Logística pós-linha",
                    rack.Status,
                    rack.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ? "warning" : "normal",
                    null,
                    new Dictionary<string, object?>
                    {
                        ["rackId"] = rack.Id,
                        ["sectionId"] = rack.SectionId,
                        ["activeAssignments"] = activeAssignments
                    }));

                if (rack.SectionId.HasValue && sectionById.TryGetValue(rack.SectionId.Value, out var rackSectionTarget))
                {
                    edges.Add(Edge(
                        $"edge-section-rack:{rackSectionTarget.SectionCode}:{rack.RackCode}",
                        SectionNodeId(rackSectionTarget),
                        RackNodeId(rack),
                        "stored-in",
                        "armazenado em",
                        null,
                        "normal"));
                }
            }
        }

        var blockedUnits = visibleUnits.Count(IsAttention);
        var topSection = visibleSections
            .Select(section => new { section, count = visibleUnits.Count(unit => unit.CurrentSectionId == section.Id) })
            .OrderByDescending(x => x.count)
            .FirstOrDefault();
        if (blockedUnits > 0)
        {
            warnings.Add(new TraceGraphWarningDto("blocked-wip", "Há unidades bloqueadas ou em retrabalho no fluxo visível.", "warning"));
        }

        var recommendation = blockedUnits > 0
            ? "Atenção: há unidades bloqueadas antes de libertar novo WIP."
            : topSection?.count > 2
                ? $"A secção {topSection.section.Name} concentra o maior WIP."
                : "Fluxo estável: não foram detetados bloqueios críticos.";

        return new TraceGraphDto(
            "Factory",
            IsOperator(access) ? "Mapa de rastreabilidade da área atribuída" : "Mapa de rastreabilidade do chão de fábrica",
            Summary(
                visibleUnits.Count > 0 ? "Active" : "Available",
                null,
                topSection?.section.Name,
                null,
                blockedUnits > 0,
                new Dictionary<string, object?>
                {
                    ["unitsInFlow"] = visibleUnits.Count,
                    ["blockedUnits"] = blockedUnits,
                    ["topWipSection"] = topSection?.section.Name,
                    ["lineTransfers"] = histories.Count(IsLineTransfer),
                    ["reworkUnits"] = visibleUnits.Count(unit => unit.Status.Equals("Rework", StringComparison.OrdinalIgnoreCase)),
                    ["reconditionedUnits"] = units.Count(unit => unit.IsReconditioned),
                    ["recoveryCandidates"] = reconditioning.Count(item => item.Status.Equals(ReconditioningStatuses.Candidate, StringComparison.OrdinalIgnoreCase) || item.Status.Equals(ReconditioningStatuses.Recoverable, StringComparison.OrdinalIgnoreCase)),
                    ["inRecovery"] = reconditioning.Count(item => item.Status.Equals(ReconditioningStatuses.InRecovery, StringComparison.OrdinalIgnoreCase)),
                    ["pendingQuality"] = visibleUnits.Count(unit => unit.QualityStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase)),
                    ["occupiedRacks"] = rackAssignments.Select(x => x.RackId).Distinct().Count(),
                    ["lastEvent"] = histories.FirstOrDefault()?.OccurredAt
                },
                recommendation),
            nodes,
            edges,
            Legend(),
            warnings);
    }

    public async Task<TraceGraphDto?> GetProductUnitGraphAsync(int id, TraceGraphAccessContext access, CancellationToken cancellationToken = default)
    {
        var unit = await _db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (unit is null) return null;

        var lines = await _db.ProductionLines.AsNoTracking().ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().ToListAsync(cancellationToken);
        if (!CanSeeUnit(unit, access, lines, sections)) throw new UnauthorizedAccessException();

        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var supportById = supports.ToDictionary(x => x.Id);
        var order = await _db.ManufacturingOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.ManufacturingOrderId, cancellationToken);
        var customer = order?.CustomerId is null ? null : await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.CustomerId.Value, cancellationToken);
        var product = order is null ? null : await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.ProductId, cancellationToken);
        var variant = unit.VariantId.HasValue ? await _db.Variants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.VariantId.Value, cancellationToken) : null;
        var locationHistory = await _db.ProductUnitLocationHistory.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.OccurredAt).ToListAsync(cancellationToken);
        var supportAssignments = await _db.UnitSupportAssignments.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.DateTimeIn).ToListAsync(cancellationToken);
        var rackAssignments = await _db.RackSupportAssignments.AsNoTracking().Where(x => unit.CurrentSupportId.HasValue && x.SupportId == unit.CurrentSupportId.Value).OrderByDescending(x => x.DateTimeIn).ToListAsync(cancellationToken);
        var racks = await _db.Racks.AsNoTracking().ToListAsync(cancellationToken);
        var rackById = racks.ToDictionary(x => x.Id);
        var quality = await _db.QualityResults.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.RecordedAt).ToListAsync(cancellationToken);
        var nonconformities = await _db.Nonconformities.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var rework = await _db.ReworkRecords.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.StartedAt).ToListAsync(cancellationToken);
        var reconditioning = await _db.ReconditionRecords.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.RecordedAt).ToListAsync(cancellationToken);
        var scrap = await _db.ScrapRecords.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.ScrappedAt).ToListAsync(cancellationToken);
        var usages = await LoadMaterialUsageAsync(new[] { unit.Id }, cancellationToken);
        var events = await _db.OperationalEvents.AsNoTracking().Where(x => x.ProductUnitId == unit.Id).OrderBy(x => x.OccurredAt).Take(30).ToListAsync(cancellationToken);

        var nodes = new List<TraceGraphNodeDto>();
        var edges = new List<TraceGraphEdgeDto>();
        var warnings = new List<TraceGraphWarningDto>();
        var currentSection = unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value) : null;
        var currentLine = currentSection?.LineId is null ? null : Find(lineById, currentSection.LineId.Value);

        if (order is not null)
        {
            nodes.Add(Node(OrderNodeId(order), "ManufacturingOrder", order.OrderNumber, "Ordem de fabrico", order.Status, OrderSeverity(order), "order", new Dictionary<string, object?>
            {
                ["orderId"] = order.Id,
                ["plannedQty"] = order.PlannedQty,
                ["scheduledUntil"] = order.ScheduledUntil,
                ["publicTrackingCode"] = CanSeeCustomer(access) ? order.PublicTrackingCode : null
            }));
            edges.Add(Edge($"edge-order-unit:{order.OrderNumber}:{unit.UnitCode}", OrderNodeId(order), UnitNodeId(unit), "produces", "produz", null, "normal"));
        }

        if (customer is not null && CanSeeCustomer(access))
        {
            nodes.Add(Node($"customer:{customer.CustomerCode}", "Customer", customer.Name, customer.CustomerCode, customer.IsActive ? "Active" : "Blocked", "normal", "order", new Dictionary<string, object?>
            {
                ["customerId"] = customer.Id,
                ["contactEmail"] = customer.ContactEmail
            }));
            if (order is not null) edges.Add(Edge($"edge-customer-order:{customer.CustomerCode}:{order.OrderNumber}", $"customer:{customer.CustomerCode}", OrderNodeId(order), "belongs-to", "cliente", null, "normal"));
        }

        if (product is not null)
        {
            nodes.Add(Node($"product:{product.Id}", "Product", product.Name, variant?.Name, null, "normal", "order", new Dictionary<string, object?>
            {
                ["productId"] = product.Id,
                ["variantCode"] = variant?.VariantCode
            }));
            edges.Add(Edge($"edge-product-unit:{product.Id}:{unit.UnitCode}", $"product:{product.Id}", UnitNodeId(unit), "product-of", "produto", null, "normal"));
        }

        nodes.Add(Node(UnitNodeId(unit), "ProductUnit", unit.UnitCode, UnitSubtitle(unit), unit.Status, IsAttention(unit) ? "warning" : "normal", currentSection?.SectionCode, new Dictionary<string, object?>
        {
            ["unitId"] = unit.Id,
            ["unitType"] = unit.UnitType,
            ["qualityStatus"] = unit.QualityStatus,
            ["isReconditioned"] = unit.IsReconditioned,
            ["reconditionedAt"] = unit.ReconditionedAt,
            ["recoveryStatus"] = unit.RecoveryStatus,
            ["qualityDisposition"] = unit.QualityDisposition,
            ["reconditionReason"] = unit.ReconditionReason,
            ["createdAt"] = unit.CreatedAt,
            ["completedAt"] = unit.CompletedAt,
            ["routeState"] = RouteState(unit, currentSection)
        }));

        var routeSections = locationHistory
            .SelectMany(x => new[] { x.FromSectionId, (int?)x.ToSectionId })
            .Concat(unit.CurrentSectionId.HasValue ? new[] { unit.CurrentSectionId } : Array.Empty<int?>())
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .Select(idValue => Find(sectionById, idValue))
            .Where(section => section is not null)
            .Cast<ProductionLineSection>()
            .ToList();

        foreach (var section in routeSections)
        {
            nodes.Add(Node(SectionNodeId(section), "Section", section.Name, section.SectionCode, unit.CurrentSectionId == section.Id ? "Current" : "Visited", unit.CurrentSectionId == section.Id ? "attention" : "history", section.LineId.HasValue && lineById.TryGetValue(section.LineId.Value, out var line) ? line.LineCode : null, new Dictionary<string, object?>
            {
                ["sectionId"] = section.Id,
                ["sectionType"] = section.SectionType,
                ["lineId"] = section.LineId
            }));
        }

        foreach (var movement in locationHistory)
        {
            var from = Find(sectionById, movement.FromSectionId);
            var to = Find(sectionById, movement.ToSectionId);
            if (from is not null && to is not null)
            {
                edges.Add(Edge($"edge-route-move:{movement.Id}", SectionNodeId(from), SectionNodeId(to), "movement", movement.EventType, movement.OccurredAt, IsLineTransfer(movement) ? "attention" : "normal", new Dictionary<string, object?>
                {
                    ["reason"] = movement.Reason,
                    ["source"] = movement.Source,
                    ["operatorUserId"] = CanSeeTechnical(access) ? movement.OperatorUserId : null
                }));
            }
            if (to is not null)
            {
                edges.Add(Edge($"edge-unit-location:{movement.Id}", UnitNodeId(unit), SectionNodeId(to), "passed-through", "passou por", movement.OccurredAt, "history"));
            }
        }

        if (currentSection is not null)
        {
            edges.Add(Edge($"edge-current-section:{unit.UnitCode}:{currentSection.SectionCode}", UnitNodeId(unit), SectionNodeId(currentSection), "current-location", "está em", null, IsAttention(unit) ? "warning" : "attention"));
        }

        foreach (var assignment in supportAssignments)
        {
            var support = Find(supportById, assignment.SupportId);
            if (support is null) continue;
            nodes.Add(Node(SupportNodeId(support), "Support", support.SupportCode, "Suporte físico", support.Status, support.Id == unit.CurrentSupportId ? "attention" : "history", null, new Dictionary<string, object?>
            {
                ["supportId"] = support.Id,
                ["dateTimeIn"] = assignment.DateTimeIn,
                ["dateTimeOut"] = assignment.DateTimeOut
            }));
            edges.Add(Edge($"edge-unit-support:{assignment.Id}", UnitNodeId(unit), SupportNodeId(support), "transported-by", "transportado por", assignment.DateTimeIn, support.Id == unit.CurrentSupportId ? "attention" : "history"));
        }

        foreach (var item in rackAssignments.Where(x => x.DateTimeOut == null).Take(3))
        {
            var rack = Find(rackById, item.RackId);
            if (rack is null) continue;
            nodes.Add(Node(RackNodeId(rack), "Rack", rack.RackCode, "Logística pós-linha", rack.Status, "normal", null, new Dictionary<string, object?> { ["rackId"] = rack.Id, ["dateTimeIn"] = item.DateTimeIn }));
            if (unit.CurrentSupportId.HasValue && supportById.TryGetValue(unit.CurrentSupportId.Value, out var support))
            {
                edges.Add(Edge($"edge-support-rack:{item.Id}", SupportNodeId(support), RackNodeId(rack), "stored-in", "armazenado em", item.DateTimeIn, "normal"));
            }
        }

        if (CanSeeMaterials(access))
        {
            foreach (var usage in usages.Where(x => x.ProductUnitId == unit.Id))
            {
                nodes.Add(Node($"lot:{usage.LotNumber}", "MaterialLot", usage.LotNumber, usage.RawMaterial, usage.AssociationType, "normal", "materials", new Dictionary<string, object?>
                {
                    ["lotId"] = usage.LotId,
                    ["rawMaterialId"] = usage.RawMaterialId,
                    ["quantity"] = usage.Quantity,
                    ["unit"] = usage.Unit
                }));
                edges.Add(Edge($"edge-lot-unit:{usage.Id}", $"lot:{usage.LotNumber}", UnitNodeId(unit), "consumed-lot", "consumiu lote", null, "normal"));
            }
        }

        if (CanSeeQuality(access))
        {
            AddQualityNodes(unit, quality, nonconformities, rework, reconditioning, scrap, nodes, edges);
        }

        if (CanSeeEvents(access))
        {
            foreach (var item in events.Take(12))
            {
                nodes.Add(Node($"event:{item.EventCode}", "Event", EventLabel(item), item.EventType, item.EventType, item.Severity ?? "history", "events", new Dictionary<string, object?>
                {
                    ["eventId"] = item.Id,
                    ["occurredAt"] = item.OccurredAt,
                    ["source"] = item.Source,
                    ["notes"] = item.Notes,
                    ["performedByUserId"] = CanSeeTechnical(access) ? item.PerformedByUserId : null
                }));
                edges.Add(Edge($"edge-unit-event:{item.EventCode}", UnitNodeId(unit), $"event:{item.EventCode}", "generated-event", "gerou evento", item.OccurredAt, item.Severity ?? "history"));
            }
        }

        if (nonconformities.Any(x => !x.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add(new TraceGraphWarningDto("open-quality-issue", "Existem não conformidades abertas para esta unidade.", "warning"));
        }

        var recommendation = IsAttention(unit)
            ? "Atenção: validar bloqueio, retrabalho ou qualidade antes de libertar a unidade."
            : unit.QualityStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                ? "Qualidade pendente: confirmar resultado antes de avançar para logística."
                : "Rota sem bloqueios críticos nos dados atuais.";

        return new TraceGraphDto(
            "ProductUnit",
            $"Rota visual da unidade {unit.UnitCode}",
            Summary(unit.Status, currentLine?.Name, currentSection?.Name, unit.QualityStatus, IsAttention(unit), new Dictionary<string, object?>
            {
                ["routeSections"] = routeSections.Count,
                ["movements"] = locationHistory.Count,
                ["lineTransfers"] = locationHistory.Count(IsLineTransfer),
                ["qualityResults"] = quality.Count,
                ["openNonconformities"] = nonconformities.Count(x => !x.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)),
                ["reconditionRecords"] = reconditioning.Count,
                ["isReconditioned"] = unit.IsReconditioned,
                ["materialLots"] = usages.Count,
                ["lastEvent"] = events.OrderByDescending(x => x.OccurredAt).FirstOrDefault()?.OccurredAt
            }, recommendation),
            DistinctNodes(nodes),
            DistinctEdges(edges),
            Legend(),
            warnings);
    }

    public async Task<TraceGraphDto?> GetManufacturingOrderGraphAsync(int id, TraceGraphAccessContext access, CancellationToken cancellationToken = default)
    {
        var order = await _db.ManufacturingOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return null;
        var customer = order.CustomerId.HasValue ? await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.CustomerId.Value, cancellationToken) : null;
        if (IsCustomer(access) && !OwnsCustomer(access, customer)) throw new UnauthorizedAccessException();

        var units = await _db.ProductUnits.AsNoTracking().Where(x => x.ManufacturingOrderId == order.Id).OrderBy(x => x.UnitCode).ToListAsync(cancellationToken);
        var unitIds = units.Select(x => x.Id).ToArray();
        var lines = await _db.ProductionLines.AsNoTracking().ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().ToListAsync(cancellationToken);
        var supports = await _db.Supports.AsNoTracking().ToListAsync(cancellationToken);
        var supportById = supports.ToDictionary(x => x.Id);
        var sectionById = sections.ToDictionary(x => x.Id);
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.ProductId, cancellationToken);
        var histories = await _db.ProductUnitLocationHistory.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).OrderBy(x => x.OccurredAt).ToListAsync(cancellationToken);
        var quality = await _db.QualityResults.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).OrderBy(x => x.RecordedAt).ToListAsync(cancellationToken);
        var nonconformities = await _db.Nonconformities.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var reconditioning = await _db.ReconditionRecords.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).OrderBy(x => x.RecordedAt).ToListAsync(cancellationToken);
        var usages = await LoadMaterialUsageAsync(unitIds, cancellationToken);

        var nodes = new List<TraceGraphNodeDto>();
        var edges = new List<TraceGraphEdgeDto>();
        var warnings = new List<TraceGraphWarningDto>();
        nodes.Add(Node(OrderNodeId(order), "ManufacturingOrder", IsCustomer(access) ? order.PublicTrackingCode ?? order.OrderNumber : order.OrderNumber, "Ordem de fabrico", order.Status, OrderSeverity(order), "order", new Dictionary<string, object?>
        {
            ["orderId"] = CanSeeTechnical(access) ? order.Id : null,
            ["plannedQty"] = order.PlannedQty,
            ["scheduledUntil"] = order.ScheduledUntil,
            ["customerReference"] = IsCustomer(access) ? null : order.CustomerReference
        }));

        if (product is not null && !IsCustomer(access))
        {
            nodes.Add(Node($"product:{product.Id}", "Product", product.Name, "Produto", null, "normal", "order", new Dictionary<string, object?> { ["productId"] = product.Id }));
            edges.Add(Edge($"edge-product-order:{product.Id}:{order.OrderNumber}", $"product:{product.Id}", OrderNodeId(order), "product-of", "produto", null, "normal"));
        }

        if (customer is not null && CanSeeCustomer(access))
        {
            nodes.Add(Node($"customer:{customer.CustomerCode}", "Customer", customer.Name, customer.CustomerCode, customer.IsActive ? "Active" : "Blocked", "normal", "order", new Dictionary<string, object?> { ["customerId"] = customer.Id }));
            edges.Add(Edge($"edge-customer-order:{customer.CustomerCode}:{order.OrderNumber}", $"customer:{customer.CustomerCode}", OrderNodeId(order), "belongs-to", "cliente", null, "normal"));
        }

        foreach (var unit in units)
        {
            var currentSection = unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value) : null;
            nodes.Add(Node(UnitNodeId(unit), "ProductUnit", unit.UnitCode, IsCustomer(access) ? CustomerProgress(unit, currentSection) : UnitSubtitle(unit), unit.Status, IsAttention(unit) ? "warning" : "normal", currentSection?.SectionCode, new Dictionary<string, object?>
            {
                ["unitId"] = CanSeeTechnical(access) ? unit.Id : null,
                ["qualityStatus"] = IsCustomer(access) ? PublicQuality(unit.QualityStatus) : unit.QualityStatus,
                ["isReconditioned"] = IsCustomer(access) ? null : unit.IsReconditioned,
                ["recoveryStatus"] = IsCustomer(access) ? null : unit.RecoveryStatus,
                ["qualityDisposition"] = IsCustomer(access) ? null : unit.QualityDisposition,
                ["routeState"] = CustomerProgress(unit, currentSection)
            }));
            edges.Add(Edge($"edge-order-unit:{order.OrderNumber}:{unit.UnitCode}", OrderNodeId(order), UnitNodeId(unit), "produces", "produz", null, IsAttention(unit) ? "warning" : "normal"));

            if (currentSection is not null)
            {
                nodes.Add(Node(SectionNodeId(currentSection), "Section", IsCustomer(access) ? PublicSectionName(currentSection) : currentSection.Name, currentSection.SectionCode, unit.Status, IsAttention(unit) ? "warning" : "normal", currentSection.LineId.HasValue ? Find(lines.ToDictionary(x => x.Id), currentSection.LineId.Value)?.LineCode : null, new Dictionary<string, object?>
                {
                    ["sectionId"] = CanSeeTechnical(access) ? currentSection.Id : null,
                    ["sectionType"] = currentSection.SectionType
                }));
                edges.Add(Edge($"edge-unit-section:{unit.UnitCode}:{currentSection.SectionCode}", UnitNodeId(unit), SectionNodeId(currentSection), "current-location", "está em", null, IsAttention(unit) ? "warning" : "attention"));
            }

            if (!IsCustomer(access) && unit.CurrentSupportId.HasValue && supportById.TryGetValue(unit.CurrentSupportId.Value, out var support))
            {
                nodes.Add(Node(SupportNodeId(support), "Support", support.SupportCode, "Suporte físico", support.Status, "normal", null, new Dictionary<string, object?> { ["supportId"] = support.Id }));
                edges.Add(Edge($"edge-unit-support-current:{unit.UnitCode}:{support.SupportCode}", UnitNodeId(unit), SupportNodeId(support), "transported-by", "transportado por", null, "normal"));
            }
        }

        if (CanSeeMaterials(access))
        {
            foreach (var usage in usages)
            {
                nodes.Add(Node($"lot:{usage.LotNumber}", "MaterialLot", usage.LotNumber, usage.RawMaterial, usage.AssociationType, "normal", "materials", new Dictionary<string, object?>
                {
                    ["lotId"] = usage.LotId,
                    ["quantity"] = usage.Quantity,
                    ["unit"] = usage.Unit
                }));
                var unit = units.FirstOrDefault(x => x.Id == usage.ProductUnitId);
                if (unit is not null)
                {
                    edges.Add(Edge($"edge-order-lot-unit:{usage.Id}", $"lot:{usage.LotNumber}", UnitNodeId(unit), "consumed-lot", "consumiu lote", null, "normal"));
                }
            }
        }

        if (CanSeeQuality(access))
        {
            foreach (var item in quality)
            {
                var unit = units.FirstOrDefault(x => x.Id == item.ProductUnitId);
                if (unit is null) continue;
                nodes.Add(Node($"quality:{item.Id}", "Quality", item.Result, item.Notes, item.Result, item.Result.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "warning" : "ok", "quality", new Dictionary<string, object?> { ["recordedAt"] = item.RecordedAt }));
                edges.Add(Edge($"edge-order-quality:{item.Id}", UnitNodeId(unit), $"quality:{item.Id}", "quality-result", "gerou qualidade", item.RecordedAt, item.Result.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "warning" : "ok"));
            }

            foreach (var item in reconditioning)
            {
                var unit = units.FirstOrDefault(x => x.Id == item.ProductUnitId);
                if (unit is null) continue;
                var severity = ReconditioningSeverity(item);
                nodes.Add(Node($"recondition:{item.Id}", "ReconditionRecord", ReconditioningLabel(item), item.Reason, item.Status, severity, "quality", new Dictionary<string, object?>
                {
                    ["recordedAt"] = item.RecordedAt,
                    ["completedAt"] = item.CompletedAt,
                    ["rejectedAt"] = item.RejectedAt,
                    ["decision"] = item.Decision,
                    ["functionalValidation"] = item.FunctionalValidation,
                    ["nextDisposition"] = item.NextDisposition
                }));
                edges.Add(Edge($"edge-order-recondition:{item.Id}", UnitNodeId(unit), $"recondition:{item.Id}", "reconditioning", "recuperação/recondicionamento", item.CompletedAt ?? item.RejectedAt ?? item.RecordedAt, severity));
            }
        }

        var blockedUnits = units.Count(IsAttention);
        if (blockedUnits > 0) warnings.Add(new TraceGraphWarningDto("order-quality-attention", "A ordem tem unidades com bloqueio, falha ou retrabalho.", "warning"));
        var recommendation = blockedUnits > 0
            ? "Atenção: resolver unidades bloqueadas antes de fechar a ordem."
            : units.Any(unit => unit.QualityStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                ? "Qualidade pendente: confirmar resultados antes de expedir."
                : "Ordem sem bloqueios críticos nos dados atuais.";

        return new TraceGraphDto(
            "ManufacturingOrder",
            $"Rastreabilidade da ordem {(IsCustomer(access) ? order.PublicTrackingCode ?? order.OrderNumber : order.OrderNumber)}",
            Summary(order.Status, null, null, null, blockedUnits > 0, new Dictionary<string, object?>
            {
                ["units"] = units.Count,
                ["completedUnits"] = units.Count(unit => unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)),
                ["blockedUnits"] = blockedUnits,
                ["lineTransfers"] = histories.Count(IsLineTransfer),
                ["qualityResults"] = quality.Count,
                ["openNonconformities"] = nonconformities.Count(x => !x.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)),
                ["reconditionedUnits"] = units.Count(unit => unit.IsReconditioned),
                ["recoveryCandidates"] = reconditioning.Count(item => item.Status.Equals(ReconditioningStatuses.Candidate, StringComparison.OrdinalIgnoreCase) || item.Status.Equals(ReconditioningStatuses.Recoverable, StringComparison.OrdinalIgnoreCase)),
                ["reconditionRecords"] = reconditioning.Count,
                ["materialLots"] = usages.Count
            }, recommendation),
            DistinctNodes(nodes),
            DistinctEdges(edges),
            Legend(),
            warnings);
    }

    public async Task<TraceGraphOptionsDto> GetOptionsAsync(TraceGraphAccessContext access, CancellationToken cancellationToken = default)
    {
        var lines = await _db.ProductionLines.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.LineCode).ToListAsync(cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var units = await _db.ProductUnits.AsNoTracking().OrderBy(x => x.UnitCode).ToListAsync(cancellationToken);
        var orders = await _db.ManufacturingOrders.AsNoTracking().OrderBy(x => x.OrderNumber).ToListAsync(cancellationToken);
        var customers = await _db.Customers.AsNoTracking().ToListAsync(cancellationToken);
        var allowedSectionIds = AllowedSectionIds(access, lines, sections);

        if (IsOperator(access))
        {
            units = units.Where(unit => unit.CurrentSectionId.HasValue && allowedSectionIds.Contains(unit.CurrentSectionId.Value)).ToList();
            var orderIds = units.Select(unit => unit.ManufacturingOrderId).Distinct().ToHashSet();
            orders = orders.Where(order => orderIds.Contains(order.Id)).ToList();
        }

        if (IsCustomer(access))
        {
            var customer = customers.FirstOrDefault(item => item.CustomerCode.Equals(access.CustomerCode ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            orders = customer is null ? new List<ManufacturingOrder>() : orders.Where(order => order.CustomerId == customer.Id).ToList();
            var orderIds = orders.Select(order => order.Id).ToHashSet();
            units = units.Where(unit => orderIds.Contains(unit.ManufacturingOrderId)).ToList();
        }

        var sectionById = sections.ToDictionary(x => x.Id);
        var lineById = lines.ToDictionary(x => x.Id);
        return new TraceGraphOptionsDto(
            units.Select(unit => new TraceGraphOptionDto(unit.Id, unit.UnitCode, $"{unit.UnitCode} · {PublicQuality(unit.QualityStatus)}", unit.Status, unit.CurrentSectionId.HasValue ? Find(sectionById, unit.CurrentSectionId.Value)?.SectionCode : null)).ToList(),
            orders.Select(order => new TraceGraphOptionDto(order.Id, IsCustomer(access) ? order.PublicTrackingCode ?? order.OrderNumber : order.OrderNumber, IsCustomer(access) ? order.PublicTrackingCode ?? order.OrderNumber : $"{order.OrderNumber} · {order.Status}", order.Status, order.CustomerId.HasValue ? customers.FirstOrDefault(x => x.Id == order.CustomerId.Value)?.CustomerCode : null)).ToList(),
            lines.Where(line => !IsOperator(access) || access.LineCode is null || line.LineCode.Equals(access.LineCode, StringComparison.OrdinalIgnoreCase)).Select(line => new TraceGraphOptionDto(line.Id, line.LineCode, line.Name, null, line.VisualGroup)).ToList(),
            units.Select(unit => unit.Status).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            units.Select(unit => unit.QualityStatus).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            new[] { "ManufacturingOrder", "ProductUnit", "ProductionLine", "Section", "Support", "Rack", "MaterialLot", "Quality", "Nonconformity", "Rework", "ReconditionRecord", "Scrap", "Event" },
            IsCustomer(access) ? new[] { "order" } : new[] { "factory", "product-unit", "order" });
    }

    public async Task<object> GetFiwareGraphStatusAsync(CancellationToken cancellationToken = default)
    {
        var context = await _fiware.GetContextAsync(cancellationToken);
        return new
        {
            coherent = context.BrokerReachable && context.EntityCount == context.RelationalSnapshotCount,
            context.BrokerReachable,
            context.EntityCount,
            context.RelationalSnapshotCount,
            context.Source,
            context.Message
        };
    }

    private async Task<List<MaterialUsageRow>> LoadMaterialUsageAsync(IEnumerable<int> unitIds, CancellationToken cancellationToken)
    {
        var ids = unitIds.Distinct().ToArray();
        return await _db.UnitMaterialLotUsages
            .AsNoTracking()
            .Where(usage => ids.Contains(usage.ProductUnitId))
            .Join(_db.LotRawMaterials, usage => usage.LotId, lot => lot.Id, (usage, lot) => new { usage, lot })
            .Join(_db.RawMaterials, item => item.lot.RawMaterialId, material => material.Id, (item, material) => new MaterialUsageRow(
                item.usage.Id,
                item.usage.ProductUnitId,
                item.lot.Id,
                item.lot.RawMaterialId,
                material.Name,
                item.lot.LotNumber,
                item.usage.AssociationType,
                item.usage.Quantity,
                item.lot.LotUnit))
            .ToListAsync(cancellationToken);
    }

    private static void AddQualityNodes(
        ProductUnit unit,
        IReadOnlyList<QualityResult> quality,
        IReadOnlyList<Nonconformity> nonconformities,
        IReadOnlyList<ReworkRecord> rework,
        IReadOnlyList<ReconditionRecord> reconditioning,
        IReadOnlyList<ScrapRecord> scrap,
        List<TraceGraphNodeDto> nodes,
        List<TraceGraphEdgeDto> edges)
    {
        foreach (var item in quality)
        {
            nodes.Add(Node($"quality:{item.Id}", "Quality", item.Result, item.Notes, item.Result, item.Result.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "warning" : "ok", "quality", new Dictionary<string, object?> { ["recordedAt"] = item.RecordedAt }));
            edges.Add(Edge($"edge-unit-quality:{item.Id}", UnitNodeId(unit), $"quality:{item.Id}", "quality-result", "gerou qualidade", item.RecordedAt, item.Result.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "warning" : "ok"));
        }

        foreach (var item in nonconformities)
        {
            nodes.Add(Node($"nonconformity:{item.Id}", "Nonconformity", item.Severity, item.Description, item.Status, "warning", "quality", new Dictionary<string, object?> { ["createdAt"] = item.CreatedAt }));
            var source = item.QualityResultId.HasValue ? $"quality:{item.QualityResultId.Value}" : UnitNodeId(unit);
            edges.Add(Edge($"edge-quality-nc:{item.Id}", source, $"nonconformity:{item.Id}", "nonconformity", "gerou não conformidade", item.CreatedAt, "warning"));
        }

        foreach (var item in rework)
        {
            nodes.Add(Node($"rework:{item.Id}", "Rework", "Retrabalho", item.Notes, item.Status, "attention", "quality", new Dictionary<string, object?> { ["startedAt"] = item.StartedAt, ["endedAt"] = item.EndedAt }));
            var source = item.NonconformityId.HasValue ? $"nonconformity:{item.NonconformityId.Value}" : UnitNodeId(unit);
            edges.Add(Edge($"edge-nc-rework:{item.Id}", source, $"rework:{item.Id}", "rework", "entrou em retrabalho", item.StartedAt, "attention"));
        }

        foreach (var item in reconditioning)
        {
            var severity = ReconditioningSeverity(item);
            nodes.Add(Node(
                $"recondition:{item.Id}",
                "ReconditionRecord",
                ReconditioningLabel(item),
                item.Reason,
                item.Status,
                severity,
                "quality",
                new Dictionary<string, object?>
                {
                    ["recordedAt"] = item.RecordedAt,
                    ["completedAt"] = item.CompletedAt,
                    ["rejectedAt"] = item.RejectedAt,
                    ["decision"] = item.Decision,
                    ["functionalValidation"] = item.FunctionalValidation,
                    ["nextDisposition"] = item.NextDisposition
                }));

            var source = item.ReworkRecordId.HasValue
                ? $"rework:{item.ReworkRecordId.Value}"
                : item.NonconformityId.HasValue
                    ? $"nonconformity:{item.NonconformityId.Value}"
                    : UnitNodeId(unit);

            edges.Add(Edge(
                $"edge-recondition-source:{item.Id}",
                source,
                $"recondition:{item.Id}",
                "reconditioning",
                "decisão de recuperação",
                item.RecordedAt,
                severity));

            edges.Add(Edge(
                $"edge-recondition-unit:{item.Id}",
                $"recondition:{item.Id}",
                UnitNodeId(unit),
                item.Status.Equals(ReconditioningStatuses.Reconditioned, StringComparison.OrdinalIgnoreCase) ? "completed-as-reconditioned" : "reconditioning-state",
                item.Status.Equals(ReconditioningStatuses.Reconditioned, StringComparison.OrdinalIgnoreCase) ? "marcou como recondicionada" : "atualizou recuperação",
                item.CompletedAt ?? item.RejectedAt ?? item.RecordedAt,
                severity));
        }

        foreach (var item in scrap)
        {
            nodes.Add(Node($"scrap:{item.Id}", "Scrap", "Sucata", item.Reason, "Scrap", "critical", "quality", new Dictionary<string, object?> { ["scrappedAt"] = item.ScrappedAt }));
            var source = item.NonconformityId.HasValue ? $"nonconformity:{item.NonconformityId.Value}" : UnitNodeId(unit);
            edges.Add(Edge($"edge-nc-scrap:{item.Id}", source, $"scrap:{item.Id}", "scrap", "registou sucata", item.ScrappedAt, "critical"));
        }
    }

    private static IReadOnlySet<int> AllowedSectionIds(TraceGraphAccessContext access, IReadOnlyList<ProductionLine> lines, IReadOnlyList<ProductionLineSection> sections)
    {
        if (!IsOperator(access)) return sections.Select(x => x.Id).ToHashSet();
        if (!string.IsNullOrWhiteSpace(access.LineCode))
        {
            var line = lines.FirstOrDefault(item => item.LineCode.Equals(access.LineCode, StringComparison.OrdinalIgnoreCase));
            if (line is not null) return sections.Where(section => section.LineId == line.Id).Select(section => section.Id).ToHashSet();
        }
        if (!string.IsNullOrWhiteSpace(access.SectionCode))
        {
            return sections.Where(section => section.SectionCode.Equals(access.SectionCode, StringComparison.OrdinalIgnoreCase)).Select(section => section.Id).ToHashSet();
        }
        return new HashSet<int>();
    }

    private static bool CanSeeUnit(ProductUnit unit, TraceGraphAccessContext access, IReadOnlyList<ProductionLine> lines, IReadOnlyList<ProductionLineSection> sections)
    {
        if (!IsOperator(access)) return true;
        if (!unit.CurrentSectionId.HasValue) return false;
        return AllowedSectionIds(access, lines, sections).Contains(unit.CurrentSectionId.Value);
    }

    private static bool CanSeeQuality(TraceGraphAccessContext access)
    {
        return access.Role is RoleNames.Administrator or RoleNames.Supervisor or RoleNames.QualityTechnician or RoleNames.DemoViewer;
    }

    private static bool CanSeeMaterials(TraceGraphAccessContext access)
    {
        return access.Role is RoleNames.Administrator or RoleNames.Supervisor or RoleNames.Logistics or RoleNames.DemoViewer;
    }

    private static bool CanSeeLogistics(TraceGraphAccessContext access)
    {
        return access.Role is RoleNames.Administrator or RoleNames.Supervisor or RoleNames.Logistics or RoleNames.DemoViewer;
    }

    private static bool CanSeeEvents(TraceGraphAccessContext access)
    {
        return !IsCustomer(access);
    }

    private static bool CanSeeCustomer(TraceGraphAccessContext access)
    {
        return access.Role is RoleNames.Administrator or RoleNames.Supervisor or RoleNames.Customer;
    }

    private static bool CanSeeTechnical(TraceGraphAccessContext access)
    {
        return access.Role is RoleNames.Administrator or RoleNames.Supervisor or RoleNames.DemoViewer;
    }

    private static bool IsOperator(TraceGraphAccessContext access)
    {
        return access.Role.Equals(RoleNames.Operator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCustomer(TraceGraphAccessContext access)
    {
        return access.Role.Equals(RoleNames.Customer, StringComparison.OrdinalIgnoreCase);
    }

    private static bool OwnsCustomer(TraceGraphAccessContext access, Customer? customer)
    {
        return customer is not null
            && !string.IsNullOrWhiteSpace(access.CustomerCode)
            && customer.CustomerCode.Equals(access.CustomerCode, StringComparison.OrdinalIgnoreCase);
    }

    private static TraceGraphNodeDto Node(string id, string type, string label, string? subtitle, string? status, string severity, string? group, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        return new TraceGraphNodeDto(id, type, label, subtitle, status, severity, group, metadata ?? new Dictionary<string, object?>());
    }

    private static TraceGraphEdgeDto Edge(string id, string source, string target, string type, string label, DateTime? timestamp, string severity, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        return new TraceGraphEdgeDto(id, source, target, type, label, timestamp, severity, metadata ?? new Dictionary<string, object?>());
    }

    private static TraceGraphSummaryDto Summary(string status, string? currentLine, string? currentSection, string? qualityStatus, bool hasOpenIssues, IReadOnlyDictionary<string, object?> metrics, string recommendation)
    {
        return new TraceGraphSummaryDto(status, currentLine, currentSection, qualityStatus, hasOpenIssues, metrics, recommendation);
    }

    private static IReadOnlyList<TraceGraphNodeDto> DistinctNodes(IEnumerable<TraceGraphNodeDto> nodes)
    {
        return nodes.GroupBy(x => x.Id).Select(x => x.First()).ToList();
    }

    private static IReadOnlyList<TraceGraphEdgeDto> DistinctEdges(IEnumerable<TraceGraphEdgeDto> edges)
    {
        return edges.GroupBy(x => x.Id).Select(x => x.First()).ToList();
    }

    private static IReadOnlyList<TraceGraphLegendItemDto> Legend()
    {
        return new List<TraceGraphLegendItemDto>
        {
            new("ProductionLine", "Linha de produção", "blue", "Agrupa secções do fluxo produtivo."),
            new("Section", "Secção", "blue", "Ponto físico do chão de fábrica."),
            new("ProductUnit", "Unidade de produto", "green", "Unidade rastreável WIP."),
            new("Support", "Suporte", "gray", "Suporte físico que transporta a unidade."),
            new("Rack", "Rack", "gray", "Logística pós-linha."),
            new("MaterialLot", "Lote de matéria-prima", "green", "Material consumido pela unidade."),
            new("Quality", "Qualidade", "yellow", "Resultado PASS/FAIL ou decisão de qualidade."),
            new("Nonconformity", "Não conformidade", "red", "Problema aberto ou histórico de qualidade."),
            new("ReconditionRecord", "Recondicionamento", "green", "Decisão e validação de recuperação produtiva."),
            new("Event", "Evento", "purple", "Evento operacional registado.")
        };
    }

    private static string SectionNodeId(ProductionLineSection section) => $"section:{section.SectionCode}";
    private static string UnitNodeId(ProductUnit unit) => $"unit:{unit.UnitCode}";
    private static string SupportNodeId(Support support) => $"support:{support.SupportCode}";
    private static string RackNodeId(Rack rack) => $"rack:{rack.RackCode}";
    private static string OrderNodeId(ManufacturingOrder order) => $"order:{order.OrderNumber}";

    private static string UnitSubtitle(ProductUnit unit)
    {
        if (unit.IsReconditioned) return $"{unit.UnitType} - Recondicionada";
        if (unit.RecoveryStatus?.Equals(ReconditioningStatuses.InRecovery, StringComparison.OrdinalIgnoreCase) == true) return $"{unit.UnitType} - Em recuperação";
        if (unit.RecoveryStatus?.Equals(ReconditioningStatuses.Candidate, StringComparison.OrdinalIgnoreCase) == true) return $"{unit.UnitType} - Candidata a recuperação";
        return $"{unit.UnitType} · Qualidade {PublicQuality(unit.QualityStatus)}";
    }

    private static string EventLabel(OperationalEvent item)
    {
        return item.EventType switch
        {
            OperationalEventTypes.ProductUnitCreated => "Unidade criada",
            OperationalEventTypes.SupportAssigned => "Suporte atribuído",
            OperationalEventTypes.SectionMovement => "Movimento de secção",
            OperationalEventTypes.LineTransfer => "Transferência de linha",
            OperationalEventTypes.QualityRecorded => "Qualidade registada",
            OperationalEventTypes.NonconformityOpened => "Não conformidade aberta",
            OperationalEventTypes.ReworkStarted => "Retrabalho iniciado",
            OperationalEventTypes.ReworkCompleted => "Retrabalho concluído",
            OperationalEventTypes.ReconditioningCandidateMarked => "Candidata a recuperação",
            OperationalEventTypes.ReconditioningStarted => "Recuperação iniciada",
            OperationalEventTypes.ReconditioningCompleted => "Recuperação concluída",
            OperationalEventTypes.ReconditioningRejected => "Recuperação rejeitada",
            OperationalEventTypes.ProductUnitMarkedReconditioned => "Produto recondicionado",
            OperationalEventTypes.ScrapRecorded => "Sucata registada",
            OperationalEventTypes.RackAssigned => "Rack atribuída",
            OperationalEventTypes.RackReleased => "Rack libertada",
            OperationalEventTypes.FiwarePublished => "Contexto FIWARE publicado",
            _ => item.EventType
        };
    }

    private static string OrderSeverity(ManufacturingOrder order)
    {
        if (order.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)) return "warning";
        if (order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "ok";
        return "normal";
    }

    private static string PublicQuality(string quality)
    {
        return quality switch
        {
            "PASS" => "Aprovado",
            "FAIL" => "Reprovado",
            "Pending" => "Pendente",
            _ => quality
        };
    }

    private static string PublicSectionName(ProductionLineSection section)
    {
        var value = $"{section.SectionType} {section.Name}".ToLowerInvariant();
        if (value.Contains("qual")) return "Controlo de qualidade";
        if (value.Contains("retrabalho")) return "Retrabalho";
        if (value.Contains("rack") || value.Contains("log")) return "Logística";
        if (value.Contains("pint")) return "Pintura";
        return "Produção";
    }

    private static string CustomerProgress(ProductUnit unit, ProductionLineSection? section)
    {
        if (unit.IsReconditioned) return "Concluído após validação";
        if (unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Concluído";
        if (unit.Status.Equals("Rework", StringComparison.OrdinalIgnoreCase)) return "Em retrabalho";
        if (unit.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) || unit.QualityStatus.Equals("FAIL", StringComparison.OrdinalIgnoreCase)) return "Em análise";
        if (section is null) return "Planeado";
        var value = $"{section.SectionType} {section.Name}".ToLowerInvariant();
        if (value.Contains("qual")) return "Em controlo de qualidade";
        if (value.Contains("rack") || value.Contains("log")) return "Em logística";
        return "Em produção";
    }

    private static bool IsClosed(ProductUnit unit)
    {
        return unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            || unit.Status.Equals("Scrap", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAttention(ProductUnit unit)
    {
        return unit.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || unit.Status.Equals("Rework", StringComparison.OrdinalIgnoreCase)
            || unit.RecoveryStatus?.Equals(ReconditioningStatuses.InRecovery, StringComparison.OrdinalIgnoreCase) == true
            || unit.QualityStatus.Equals("FAIL", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReconditioningLabel(ReconditionRecord item)
    {
        return item.Status switch
        {
            ReconditioningStatuses.Candidate => "Candidata a recuperação",
            ReconditioningStatuses.Recoverable => "Recuperável",
            ReconditioningStatuses.InRecovery => "Em recuperação",
            ReconditioningStatuses.Reconditioned => "Recondicionada",
            ReconditioningStatuses.Rejected => "Recuperação rejeitada",
            _ => "Recuperação"
        };
    }

    private static string ReconditioningSeverity(ReconditionRecord item)
    {
        return item.Status switch
        {
            ReconditioningStatuses.Reconditioned => "ok",
            ReconditioningStatuses.Rejected => "critical",
            ReconditioningStatuses.InRecovery => "attention",
            ReconditioningStatuses.Candidate => "warning",
            ReconditioningStatuses.Recoverable => "warning",
            _ => "history"
        };
    }

    private static bool IsLineTransfer(ProductUnitLocationHistory item)
    {
        return item.EventType.Equals("LineTransfer", StringComparison.OrdinalIgnoreCase)
            || (item.FromProductionLineId.HasValue && item.ToProductionLineId.HasValue && item.FromProductionLineId != item.ToProductionLineId);
    }

    private static string RouteState(ProductUnit unit, ProductionLineSection? section)
    {
        if (unit.IsReconditioned) return "Recondicionada";
        if (unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Concluído";
        if (unit.Status.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) return "Sucata";
        if (IsAttention(unit)) return "Atenção";
        if (section is null) return "Sem localização";
        if (section.IsTransferPoint) return "Ponto de transferência";
        return "Em linha";
    }

    private static T? Find<T>(IReadOnlyDictionary<int, T> items, int? id)
    {
        return id.HasValue && items.TryGetValue(id.Value, out var value) ? value : default;
    }

    private sealed record MaterialUsageRow(
        int Id,
        int ProductUnitId,
        int LotId,
        int RawMaterialId,
        string RawMaterial,
        string LotNumber,
        string AssociationType,
        int Quantity,
        string Unit);
}
