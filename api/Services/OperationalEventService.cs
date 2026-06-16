using System.Text.Json;
using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public static class OperationalEventTypes
{
    public const string ProductUnitCreated = nameof(ProductUnitCreated);
    public const string SupportAssigned = nameof(SupportAssigned);
    public const string SectionMovement = nameof(SectionMovement);
    public const string LineTransfer = nameof(LineTransfer);
    public const string QualityRecorded = nameof(QualityRecorded);
    public const string NonconformityOpened = nameof(NonconformityOpened);
    public const string ReworkStarted = nameof(ReworkStarted);
    public const string ReworkCompleted = nameof(ReworkCompleted);
    public const string ReconditioningCandidateMarked = nameof(ReconditioningCandidateMarked);
    public const string ReconditioningStarted = nameof(ReconditioningStarted);
    public const string ReconditioningCompleted = nameof(ReconditioningCompleted);
    public const string ReconditioningRejected = nameof(ReconditioningRejected);
    public const string ProductUnitMarkedReconditioned = nameof(ProductUnitMarkedReconditioned);
    public const string ScrapRecorded = nameof(ScrapRecorded);
    public const string RackAssigned = nameof(RackAssigned);
    public const string RackReleased = nameof(RackReleased);
    public const string FiwarePublished = nameof(FiwarePublished);
    public const string DemoSeeded = nameof(DemoSeeded);
    public const string SimulationRunCreated = nameof(SimulationRunCreated);
    public const string SimulationStepExecuted = nameof(SimulationStepExecuted);
    public const string SimulationPaused = nameof(SimulationPaused);
    public const string SimulationResumed = nameof(SimulationResumed);
    public const string SimulationStopped = nameof(SimulationStopped);
    public const string SimulationCompleted = nameof(SimulationCompleted);
    public const string SimulationProductUnitAdvanced = nameof(SimulationProductUnitAdvanced);
    public const string SimulationQualityFailureInjected = nameof(SimulationQualityFailureInjected);
    public const string SimulationReconditioningPathExecuted = nameof(SimulationReconditioningPathExecuted);
    public const string SimulationScrapPathExecuted = nameof(SimulationScrapPathExecuted);
    public const string SimulationLineTransferExecuted = nameof(SimulationLineTransferExecuted);

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ProductUnitCreated,
        SupportAssigned,
        SectionMovement,
        LineTransfer,
        QualityRecorded,
        NonconformityOpened,
        ReworkStarted,
        ReworkCompleted,
        ReconditioningCandidateMarked,
        ReconditioningStarted,
        ReconditioningCompleted,
        ReconditioningRejected,
        ProductUnitMarkedReconditioned,
        ScrapRecorded,
        RackAssigned,
        RackReleased,
        FiwarePublished,
        DemoSeeded,
        SimulationRunCreated,
        SimulationStepExecuted,
        SimulationPaused,
        SimulationResumed,
        SimulationStopped,
        SimulationCompleted,
        SimulationProductUnitAdvanced,
        SimulationQualityFailureInjected,
        SimulationReconditioningPathExecuted,
        SimulationScrapPathExecuted,
        SimulationLineTransferExecuted
    };
}

public static class OperationalEventSources
{
    public const string Api = nameof(Api);
    public const string Manual = nameof(Manual);
    public const string Seed = nameof(Seed);
    public const string Playback = nameof(Playback);
    public const string FiwareSync = nameof(FiwareSync);
    public const string Simulation = nameof(Simulation);
}

public sealed class OperationalEventCreateRequest
{
    public string? EventCode { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int? ProductUnitId { get; set; }
    public int? SupportId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public int? FromProductionLineId { get; set; }
    public int? ToProductionLineId { get; set; }
    public int? FromSectionId { get; set; }
    public int? ToSectionId { get; set; }
    public int? CheckpointId { get; set; }
    public int? QualityResultId { get; set; }
    public int? NonconformityId { get; set; }
    public int? ReworkRecordId { get; set; }
    public int? ReconditionRecordId { get; set; }
    public int? ScrapRecordId { get; set; }
    public int? RackId { get; set; }
    public string? ReasonCode { get; set; }
    public string? Severity { get; set; }
    public string Source { get; set; } = OperationalEventSources.Api;
    public string? PerformedByUserId { get; set; }
    public DateTime? OccurredAt { get; set; }
    public string? Notes { get; set; }
    public bool IsDemo { get; set; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; set; }
}

public sealed class OperationalEventQueryRequest
{
    public string? EventType { get; set; }
    public int? ProductUnitId { get; set; }
    public int? SupportId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public bool? IsDemo { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Limit { get; set; } = 100;
}

public sealed class OperationalEventDto
{
    public int Id { get; init; }
    public string EventCode { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public object? ProductUnit { get; init; }
    public object? Support { get; init; }
    public object? ManufacturingOrder { get; init; }
    public object? FromProductionLine { get; init; }
    public object? ToProductionLine { get; init; }
    public object? FromSection { get; init; }
    public object? ToSection { get; init; }
    public object? Checkpoint { get; init; }
    public object? QualityResult { get; init; }
    public object? Nonconformity { get; init; }
    public object? ReworkRecord { get; init; }
    public object? ReconditionRecord { get; init; }
    public object? ScrapRecord { get; init; }
    public object? Rack { get; init; }
    public string? ReasonCode { get; init; }
    public string? Severity { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? PerformedByUserId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? Notes { get; init; }
    public bool IsDemo { get; init; }
    public object? Metadata { get; init; }
    public string Label { get; init; } = string.Empty;
}

public sealed class OperationalEventService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DriveTraceDbContext _db;

    public OperationalEventService(DriveTraceDbContext db)
    {
        _db = db;
    }

    public async Task<OperationalEventDto> RecordAsync(
        OperationalEventCreateRequest request,
        bool saveChanges = true,
        CancellationToken cancellationToken = default)
    {
        ValidateEventType(request.EventType);
        await ValidateReferencesAsync(request, cancellationToken);

        var eventCode = string.IsNullOrWhiteSpace(request.EventCode)
            ? await GenerateEventCodeAsync(cancellationToken)
            : Clean(request.EventCode, 80);

        var existing = await _db.OperationalEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EventCode == eventCode, cancellationToken);
        if (existing is not null)
        {
            return (await ToDtosAsync(new[] { existing }, cancellationToken)).Single();
        }

        var operationalEvent = new OperationalEvent
        {
            EventCode = eventCode,
            EventType = Clean(request.EventType, 80),
            ProductUnitId = request.ProductUnitId,
            SupportId = request.SupportId,
            ManufacturingOrderId = request.ManufacturingOrderId,
            FromProductionLineId = request.FromProductionLineId,
            ToProductionLineId = request.ToProductionLineId,
            FromSectionId = request.FromSectionId,
            ToSectionId = request.ToSectionId,
            CheckpointId = request.CheckpointId,
            QualityResultId = request.QualityResultId,
            NonconformityId = request.NonconformityId,
            ReworkRecordId = request.ReworkRecordId,
            ReconditionRecordId = request.ReconditionRecordId,
            ScrapRecordId = request.ScrapRecordId,
            RackId = request.RackId,
            ReasonCode = CleanNullable(request.ReasonCode, 80),
            Severity = CleanNullable(request.Severity, 40),
            Source = Clean(request.Source, 40, OperationalEventSources.Api),
            PerformedByUserId = CleanNullable(request.PerformedByUserId, 120),
            OccurredAt = NormalizeUtc(request.OccurredAt ?? DateTime.UtcNow),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsDemo = request.IsDemo,
            MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata, JsonOptions)
        };

        _db.OperationalEvents.Add(operationalEvent);
        if (saveChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (await ToDtosAsync(new[] { operationalEvent }, cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<OperationalEventDto>> QueryAsync(
        OperationalEventQueryRequest request,
        bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit <= 0 ? 100 : request.Limit, 1, 500);
        var query = _db.OperationalEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query = query.Where(x => x.EventType == request.EventType.Trim());
        }

        if (request.ProductUnitId.HasValue)
        {
            query = query.Where(x => x.ProductUnitId == request.ProductUnitId.Value);
        }

        if (request.SupportId.HasValue)
        {
            query = query.Where(x => x.SupportId == request.SupportId.Value);
        }

        if (request.ManufacturingOrderId.HasValue)
        {
            query = query.Where(x => x.ManufacturingOrderId == request.ManufacturingOrderId.Value);
        }

        if (request.IsDemo.HasValue)
        {
            query = query.Where(x => x.IsDemo == request.IsDemo.Value);
        }

        if (request.From.HasValue)
        {
            var from = NormalizeUtc(request.From.Value);
            query = query.Where(x => x.OccurredAt >= from);
        }

        if (request.To.HasValue)
        {
            var to = NormalizeUtc(request.To.Value);
            query = query.Where(x => x.OccurredAt <= to);
        }

        var events = ascending
            ? await query.OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).Take(limit).ToListAsync(cancellationToken)
            : await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Take(limit).ToListAsync(cancellationToken);

        return await ToDtosAsync(events, cancellationToken);
    }

    public Task<IReadOnlyList<OperationalEventDto>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        return QueryAsync(new OperationalEventQueryRequest { Limit = limit <= 0 ? 20 : limit }, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<OperationalEventDto>> GetForProductUnitAsync(
        int productUnitId,
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        return QueryAsync(new OperationalEventQueryRequest { ProductUnitId = productUnitId, Limit = limit }, ascending: true, cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<OperationalEventDto>> ToDtosAsync(
        IReadOnlyCollection<OperationalEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0) return Array.Empty<OperationalEventDto>();

        var productUnits = await LoadDictionaryAsync(_db.ProductUnits, events.Select(x => x.ProductUnitId), cancellationToken);
        var supports = await LoadDictionaryAsync(_db.Supports, events.Select(x => x.SupportId), cancellationToken);
        var orders = await LoadDictionaryAsync(_db.ManufacturingOrders, events.Select(x => x.ManufacturingOrderId), cancellationToken);
        var lines = await LoadDictionaryAsync(
            _db.ProductionLines,
            events.Select(x => x.FromProductionLineId).Concat(events.Select(x => x.ToProductionLineId)),
            cancellationToken);
        var sections = await LoadDictionaryAsync(
            _db.ProductionLineSections,
            events.Select(x => x.FromSectionId).Concat(events.Select(x => x.ToSectionId)),
            cancellationToken);
        var checkpoints = await LoadDictionaryAsync(_db.Checkpoints, events.Select(x => x.CheckpointId), cancellationToken);
        var qualityResults = await LoadDictionaryAsync(_db.QualityResults, events.Select(x => x.QualityResultId), cancellationToken);
        var nonconformities = await LoadDictionaryAsync(_db.Nonconformities, events.Select(x => x.NonconformityId), cancellationToken);
        var reworkRecords = await LoadDictionaryAsync(_db.ReworkRecords, events.Select(x => x.ReworkRecordId), cancellationToken);
        var reconditionRecords = await LoadDictionaryAsync(_db.ReconditionRecords, events.Select(x => x.ReconditionRecordId), cancellationToken);
        var scrapRecords = await LoadDictionaryAsync(_db.ScrapRecords, events.Select(x => x.ScrapRecordId), cancellationToken);
        var racks = await LoadDictionaryAsync(_db.Racks, events.Select(x => x.RackId), cancellationToken);

        return events.Select(item =>
        {
            var productUnit = Find(productUnits, item.ProductUnitId);
            var support = Find(supports, item.SupportId);
            var order = Find(orders, item.ManufacturingOrderId);
            var toSection = Find(sections, item.ToSectionId);
            var fromSection = Find(sections, item.FromSectionId);
            var toLine = Find(lines, item.ToProductionLineId) ?? (toSection?.LineId is null ? null : Find(lines, toSection.LineId.Value));
            var fromLine = Find(lines, item.FromProductionLineId) ?? (fromSection?.LineId is null ? null : Find(lines, fromSection.LineId.Value));
            var checkpoint = Find(checkpoints, item.CheckpointId);
            var quality = Find(qualityResults, item.QualityResultId);
            var nonconformity = Find(nonconformities, item.NonconformityId);
            var rework = Find(reworkRecords, item.ReworkRecordId);
            var reconditioning = Find(reconditionRecords, item.ReconditionRecordId);
            var scrap = Find(scrapRecords, item.ScrapRecordId);
            var rack = Find(racks, item.RackId);

            return new OperationalEventDto
            {
                Id = item.Id,
                EventCode = item.EventCode,
                EventType = item.EventType,
                ProductUnit = productUnit is null ? null : new { id = productUnit.Id, code = productUnit.UnitCode, status = productUnit.Status },
                Support = support is null ? null : new { id = support.Id, code = support.SupportCode, status = support.Status },
                ManufacturingOrder = order is null ? null : new { id = order.Id, code = order.OrderNumber, status = order.Status },
                FromProductionLine = fromLine is null ? null : new { id = fromLine.Id, code = fromLine.LineCode, name = fromLine.Name },
                ToProductionLine = toLine is null ? null : new { id = toLine.Id, code = toLine.LineCode, name = toLine.Name },
                FromSection = fromSection is null ? null : new { id = fromSection.Id, code = fromSection.SectionCode, name = fromSection.Name, type = fromSection.SectionType },
                ToSection = toSection is null ? null : new { id = toSection.Id, code = toSection.SectionCode, name = toSection.Name, type = toSection.SectionType },
                Checkpoint = checkpoint is null ? null : new { id = checkpoint.Id, code = checkpoint.CheckpointCode, name = checkpoint.Name },
                QualityResult = quality is null ? null : new { id = quality.Id, result = quality.Result, recordedAt = quality.RecordedAt },
                Nonconformity = nonconformity is null ? null : new { id = nonconformity.Id, status = nonconformity.Status, severity = nonconformity.Severity },
                ReworkRecord = rework is null ? null : new { id = rework.Id, status = rework.Status, startedAt = rework.StartedAt, endedAt = rework.EndedAt },
                ReconditionRecord = reconditioning is null ? null : new { id = reconditioning.Id, status = reconditioning.Status, decision = reconditioning.Decision, completedAt = reconditioning.CompletedAt },
                ScrapRecord = scrap is null ? null : new { id = scrap.Id, scrappedAt = scrap.ScrappedAt, reason = scrap.Reason },
                Rack = rack is null ? null : new { id = rack.Id, code = rack.RackCode, status = rack.Status },
                ReasonCode = item.ReasonCode,
                Severity = item.Severity,
                Source = item.Source,
                PerformedByUserId = item.PerformedByUserId,
                OccurredAt = item.OccurredAt,
                Notes = item.Notes,
                IsDemo = item.IsDemo,
                Metadata = ParseMetadata(item.MetadataJson),
                Label = BuildLabel(item, productUnit, support, toSection, rack)
            };
        }).ToList();
    }

    private async Task ValidateReferencesAsync(OperationalEventCreateRequest request, CancellationToken cancellationToken)
    {
        await EnsureExistsAsync(_db.ProductUnits, request.ProductUnitId, "unidade de produto", cancellationToken);
        await EnsureExistsAsync(_db.Supports, request.SupportId, "suporte", cancellationToken);
        await EnsureExistsAsync(_db.ManufacturingOrders, request.ManufacturingOrderId, "ordem de fabrico", cancellationToken);
        await EnsureExistsAsync(_db.ProductionLines, request.FromProductionLineId, "linha de origem", cancellationToken);
        await EnsureExistsAsync(_db.ProductionLines, request.ToProductionLineId, "linha de destino", cancellationToken);
        await EnsureExistsAsync(_db.ProductionLineSections, request.FromSectionId, "secção de origem", cancellationToken);
        await EnsureExistsAsync(_db.ProductionLineSections, request.ToSectionId, "secção de destino", cancellationToken);
        await EnsureExistsAsync(_db.Checkpoints, request.CheckpointId, "ponto de controlo", cancellationToken);
        await EnsureExistsAsync(_db.QualityResults, request.QualityResultId, "resultado de qualidade", cancellationToken);
        await EnsureExistsAsync(_db.Nonconformities, request.NonconformityId, "não conformidade", cancellationToken);
        await EnsureExistsAsync(_db.ReworkRecords, request.ReworkRecordId, "registo de retrabalho", cancellationToken);
        await EnsureExistsAsync(_db.ReconditionRecords, request.ReconditionRecordId, "registo de recondicionamento", cancellationToken);
        await EnsureExistsAsync(_db.ScrapRecords, request.ScrapRecordId, "registo de sucata", cancellationToken);
        await EnsureExistsAsync(_db.Racks, request.RackId, "rack", cancellationToken);
    }

    private static void ValidateEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType) || !OperationalEventTypes.All.Contains(eventType.Trim()))
        {
            throw new InvalidOperationException($"Tipo de evento operacional não suportado: '{eventType}'.");
        }
    }

    private async Task<string> GenerateEventCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"EVT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..30];
            if (!await _db.OperationalEvents.AnyAsync(x => x.EventCode == code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código único para o evento operacional.");
    }

    private static async Task EnsureExistsAsync<TEntity>(
        DbSet<TEntity> set,
        int? id,
        string label,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity
    {
        if (!id.HasValue) return;
        if (id.Value <= 0)
        {
            throw new InvalidOperationException($"A referência para {label} tem um identificador inválido.");
        }

        if (!await set.AnyAsync(x => x.Id == id.Value, cancellationToken))
        {
            throw new InvalidOperationException($"A referência para {label} com id {id.Value} não existe.");
        }
    }

    private static async Task<Dictionary<int, TEntity>> LoadDictionaryAsync<TEntity>(
        DbSet<TEntity> set,
        IEnumerable<int?> ids,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity
    {
        var idArray = ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (idArray.Length == 0) return new Dictionary<int, TEntity>();
        return await set.AsNoTracking().Where(x => idArray.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private static TEntity? Find<TEntity>(IReadOnlyDictionary<int, TEntity> items, int? id)
    {
        return id.HasValue && items.TryGetValue(id.Value, out var value) ? value : default;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc) return value;
        if (value.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return value.ToUniversalTime();
    }

    private static string Clean(string? value, int maxLength, string fallback = "")
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string? CleanNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static object? ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson, JsonOptions);
        }
        catch
        {
            return metadataJson;
        }
    }

    private static string BuildLabel(
        OperationalEvent item,
        ProductUnit? productUnit,
        Support? support,
        ProductionLineSection? toSection,
        Rack? rack)
    {
        var target = productUnit?.UnitCode ?? support?.SupportCode ?? rack?.RackCode;
        var location = toSection is null ? null : $" em {toSection.SectionCode}";
        return item.EventType switch
        {
            OperationalEventTypes.ProductUnitCreated => $"Unidade {target} criada",
            OperationalEventTypes.SupportAssigned => $"Suporte {support?.SupportCode} atribuído a {productUnit?.UnitCode}",
            OperationalEventTypes.SectionMovement => $"Movimento de {target}{location}",
            OperationalEventTypes.LineTransfer => $"Transferência de {target}{location}",
            OperationalEventTypes.QualityRecorded => $"Qualidade registada para {target}",
            OperationalEventTypes.NonconformityOpened => $"Não conformidade aberta para {target}",
            OperationalEventTypes.ReworkStarted => $"Retrabalho iniciado para {target}",
            OperationalEventTypes.ReworkCompleted => $"Retrabalho concluído para {target}",
            OperationalEventTypes.ReconditioningCandidateMarked => $"Unidade {target} marcada como recuperável",
            OperationalEventTypes.ReconditioningStarted => $"Recuperação iniciada para {target}",
            OperationalEventTypes.ReconditioningCompleted => $"Recuperação concluída para {target}",
            OperationalEventTypes.ReconditioningRejected => $"Recuperação rejeitada para {target}",
            OperationalEventTypes.ProductUnitMarkedReconditioned => $"Unidade {target} marcada como recondicionada",
            OperationalEventTypes.ScrapRecorded => $"Sucata registada para {target}",
            OperationalEventTypes.RackAssigned => $"Rack {rack?.RackCode} atribuída a {support?.SupportCode ?? target}",
            OperationalEventTypes.RackReleased => $"Rack {rack?.RackCode} libertada",
            OperationalEventTypes.FiwarePublished => "Contexto FIWARE publicado",
            OperationalEventTypes.DemoSeeded => "Dados demonstrativos inicializados",
            OperationalEventTypes.SimulationRunCreated => $"Simulação criada para {target}",
            OperationalEventTypes.SimulationStepExecuted => $"Passo de simulação executado para {target}",
            OperationalEventTypes.SimulationPaused => $"Simulação pausada para {target}",
            OperationalEventTypes.SimulationResumed => $"Simulação retomada para {target}",
            OperationalEventTypes.SimulationStopped => $"Simulação terminada para {target}",
            OperationalEventTypes.SimulationCompleted => $"Simulação concluída para {target}",
            OperationalEventTypes.SimulationProductUnitAdvanced => $"Unidade avançada por simulação: {target}",
            OperationalEventTypes.SimulationQualityFailureInjected => $"Falha de qualidade injetada para {target}",
            OperationalEventTypes.SimulationReconditioningPathExecuted => $"Percurso de recondicionamento executado para {target}",
            OperationalEventTypes.SimulationScrapPathExecuted => $"Percurso de sucata executado para {target}",
            OperationalEventTypes.SimulationLineTransferExecuted => $"Transferência de linha executada para {target}",
            _ => item.EventType
        };
    }
}
