using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public static class ReconditioningStatuses
{
    public const string None = "None";
    public const string Candidate = "Candidate";
    public const string Recoverable = "Recoverable";
    public const string InRecovery = "InRecovery";
    public const string Reconditioned = "Reconditioned";
    public const string Rejected = "Rejected";
}

public static class QualityDispositions
{
    public const string Normal = "Normal";
    public const string Pending = "Pending";
    public const string Failed = "Failed";
    public const string Recoverable = "Recoverable";
    public const string Reconditioned = "Reconditioned";
    public const string Scrap = "Scrap";
    public const string Blocked = "Blocked";
}

public sealed record ReconditioningAccessContext(
    string Username,
    string Role,
    string? LineCode,
    string? SectionCode,
    string? CustomerCode);

public sealed class ReconditioningService
{
    private static readonly string[] RecoverableSeverities =
    [
        "Minor",
        "Menor",
        "Medium",
        "Media",
        "Média",
        "Recoverable",
        "Recuperavel",
        "Recuperável"
    ];

    private readonly DriveTraceDbContext _db;
    private readonly OperationalEventService _events;

    public ReconditioningService(DriveTraceDbContext db, OperationalEventService events)
    {
        _db = db;
        _events = events;
    }

    public async Task<ReconditioningListDto> GetListAsync(
        ReconditioningQuery query,
        ReconditioningAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        var items = BuildItems(context, access).ToList();
        items = ApplyFilters(items, query).ToList();
        return new ReconditioningListDto(BuildSummary(items), items);
    }

    public async Task<ReconditioningListDto> GetCandidatesAsync(
        ReconditioningAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        var items = BuildItems(context, access)
            .Where(item => item.CanMarkRecoverable || item.CanComplete || IsCandidateStatus(item.RecoveryStatus))
            .ToList();

        return new ReconditioningListDto(BuildSummary(items), items);
    }

    public async Task<ReconditioningItemDto?> GetByIdAsync(
        int id,
        ReconditioningAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        var items = BuildItems(context, access).ToList();
        return items.FirstOrDefault(item => item.Id == id)
            ?? items.FirstOrDefault(item => item.ProductUnitId == id);
    }

    public async Task<ReconditioningItemDto> MarkRecoverableAsync(
        int productUnitId,
        ReconditioningActionRequest request,
        ReconditioningAccessContext access,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var unit = await LoadUnitForDecisionAsync(productUnitId, cancellationToken);
        var nonconformity = await ResolveRecoverableNonconformityAsync(unit.Id, request.NonconformityId, cancellationToken);
        await EnsureCanRecoverAsync(unit, nonconformity, cancellationToken);

        var rework = await ResolveOrCreateReworkAsync(unit.Id, nonconformity.Id, request.ReworkRecordId, now, cancellationToken);
        var record = await ResolveOrCreateReconditioningRecordAsync(unit.Id, nonconformity.Id, now, cancellationToken);

        record.ReworkRecordId = rework.Id;
        record.Status = ReconditioningStatuses.InRecovery;
        record.Decision = ReconditioningStatuses.Recoverable;
        record.Reason = CleanRequired(request.Reason, "Indique a justificação para marcar a unidade como recuperável.");
        record.Notes = CleanOptional(request.Notes);
        record.FunctionalValidation = false;
        record.RecordedByResourceId = request.RecordedByResourceId;
        record.PerformedByUserId = access.Username;
        record.NextDisposition = null;
        record.CompletedAt = null;
        record.RejectedAt = null;

        unit.Status = "Rework";
        unit.QualityStatus = "FAIL";
        unit.IsReconditioned = false;
        unit.ReconditionedAt = null;
        unit.ReconditionReason = null;
        unit.ReconditionedFromNonconformityId = null;
        unit.ReconditionedByResourceId = null;
        unit.RecoveryStatus = ReconditioningStatuses.InRecovery;
        unit.QualityDisposition = QualityDispositions.Recoverable;
        nonconformity.Status = ReconditioningStatuses.Recoverable;

        await _db.SaveChangesAsync(cancellationToken);

        await RecordReconditioningEventAsync(
            OperationalEventTypes.ReconditioningCandidateMarked,
            unit,
            nonconformity,
            rework,
            record,
            access,
            now,
            "Unidade marcada como recuperável após não conformidade recuperável.",
            cancellationToken);

        await RecordReconditioningEventAsync(
            OperationalEventTypes.ReconditioningStarted,
            unit,
            nonconformity,
            rework,
            record,
            access,
            now.AddMilliseconds(1),
            "Recuperação produtiva iniciada.",
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetRequiredByUnitAsync(unit.Id, access, cancellationToken);
    }

    public async Task<ReconditioningItemDto> CompleteAsync(
        int productUnitId,
        ReconditioningActionRequest request,
        ReconditioningAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (!request.FunctionalValidation)
        {
            throw new InvalidOperationException("A validação funcional final é obrigatória para concluir o recondicionamento.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var unit = await LoadUnitForDecisionAsync(productUnitId, cancellationToken);
        var nonconformity = await ResolveRecoverableNonconformityAsync(unit.Id, request.NonconformityId, cancellationToken);
        await EnsureCanRecoverAsync(unit, nonconformity, cancellationToken);

        var rework = await ResolveOrCreateReworkAsync(unit.Id, nonconformity.Id, request.ReworkRecordId, now, cancellationToken);
        rework.Status = "Completed";
        rework.EndedAt ??= now;
        rework.Notes = MergeNotes(rework.Notes, request.Notes);

        var record = await ResolveOrCreateReconditioningRecordAsync(unit.Id, nonconformity.Id, now, cancellationToken);
        record.ReworkRecordId = rework.Id;
        record.Status = ReconditioningStatuses.Reconditioned;
        record.Decision = ReconditioningStatuses.Reconditioned;
        record.Reason = CleanRequired(request.Reason, "Indique a justificação para concluir o recondicionamento.");
        record.Notes = CleanOptional(request.Notes);
        record.FunctionalValidation = true;
        record.CompletedAt = now;
        record.RejectedAt = null;
        record.RecordedByResourceId = request.RecordedByResourceId;
        record.PerformedByUserId = access.Username;
        record.NextDisposition = QualityDispositions.Reconditioned;

        unit.Status = "Completed";
        unit.QualityStatus = "PASS";
        unit.IsReconditioned = true;
        unit.ReconditionedAt = now;
        unit.ReconditionReason = record.Reason;
        unit.ReconditionedFromNonconformityId = nonconformity.Id;
        unit.ReconditionedByResourceId = request.RecordedByResourceId;
        unit.CompletedAt ??= now;
        unit.RecoveryStatus = ReconditioningStatuses.Reconditioned;
        unit.QualityDisposition = QualityDispositions.Reconditioned;
        nonconformity.Status = "Closed";

        await _db.SaveChangesAsync(cancellationToken);

        await RecordReconditioningEventAsync(
            OperationalEventTypes.ReconditioningCompleted,
            unit,
            nonconformity,
            rework,
            record,
            access,
            now,
            "Recuperação concluída com validação funcional final.",
            cancellationToken);

        await RecordReconditioningEventAsync(
            OperationalEventTypes.ProductUnitMarkedReconditioned,
            unit,
            nonconformity,
            rework,
            record,
            access,
            now.AddMilliseconds(1),
            "Unidade marcada como produto recondicionado.",
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetRequiredByUnitAsync(unit.Id, access, cancellationToken);
    }

    public async Task<ReconditioningItemDto> RejectAsync(
        int productUnitId,
        ReconditioningRejectRequest request,
        ReconditioningAccessContext access,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var unit = await LoadUnitForDecisionAsync(productUnitId, cancellationToken);
        var nonconformity = await ResolveNonconformityForRejectionAsync(unit.Id, request.NonconformityId, cancellationToken);

        if (unit.IsReconditioned)
        {
            throw new InvalidOperationException("Uma unidade já recondicionada não pode ser rejeitada nesta decisão.");
        }

        var disposition = NormalizeNextDisposition(request.NextDisposition);
        var record = await ResolveOrCreateReconditioningRecordAsync(unit.Id, nonconformity.Id, now, cancellationToken);
        var rework = await ResolveLatestReworkAsync(unit.Id, nonconformity.Id, cancellationToken);
        record.ReworkRecordId = rework?.Id;
        record.Status = ReconditioningStatuses.Rejected;
        record.Decision = ReconditioningStatuses.Rejected;
        record.Reason = CleanRequired(request.Reason, "Indique a justificação para rejeitar a recuperação.");
        record.Notes = CleanOptional(request.Notes);
        record.FunctionalValidation = false;
        record.RejectedAt = now;
        record.CompletedAt = null;
        record.PerformedByUserId = access.Username;
        record.NextDisposition = disposition;

        unit.IsReconditioned = false;
        unit.ReconditionedAt = null;
        unit.ReconditionReason = null;
        unit.ReconditionedFromNonconformityId = null;
        unit.ReconditionedByResourceId = null;
        unit.RecoveryStatus = ReconditioningStatuses.Rejected;
        unit.QualityDisposition = disposition;

        ScrapRecord? scrap = null;
        if (disposition == QualityDispositions.Scrap)
        {
            scrap = await _db.ScrapRecords.FirstOrDefaultAsync(
                item => item.ProductUnitId == unit.Id && item.NonconformityId == nonconformity.Id,
                cancellationToken);
            if (scrap is null)
            {
                scrap = new ScrapRecord
                {
                    ProductUnitId = unit.Id,
                    NonconformityId = nonconformity.Id,
                    ScrappedAt = now,
                    Reason = record.Reason
                };
                _db.ScrapRecords.Add(scrap);
            }

            unit.Status = "Scrap";
            unit.QualityStatus = "FAIL";
            nonconformity.Status = "Scrap";
        }
        else if (disposition == QualityDispositions.Recoverable)
        {
            unit.Status = "Rework";
            unit.QualityStatus = "FAIL";
            nonconformity.Status = "Rework";
        }
        else
        {
            unit.Status = "Blocked";
            unit.QualityStatus = "FAIL";
            nonconformity.Status = "Blocked";
        }

        await _db.SaveChangesAsync(cancellationToken);

        await RecordReconditioningEventAsync(
            OperationalEventTypes.ReconditioningRejected,
            unit,
            nonconformity,
            rework,
            record,
            access,
            now,
            $"Recuperação rejeitada. Disposição seguinte: {DisplayDisposition(disposition)}.",
            cancellationToken);

        if (scrap is not null)
        {
            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"RECOND-SCRAP-{record.Id}-{now:yyyyMMddHHmmssfff}",
                EventType = OperationalEventTypes.ScrapRecorded,
                ProductUnitId = unit.Id,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                NonconformityId = nonconformity.Id,
                ReworkRecordId = rework?.Id,
                ReconditionRecordId = record.Id,
                ScrapRecordId = scrap.Id,
                ReasonCode = disposition,
                Severity = nonconformity.Severity,
                Source = OperationalEventSources.Api,
                PerformedByUserId = access.Username,
                OccurredAt = now.AddMilliseconds(1),
                Notes = record.Reason,
                Metadata = new Dictionary<string, object?>
                {
                    ["decision"] = record.Decision,
                    ["nextDisposition"] = disposition
                }
            }, saveChanges: false, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetRequiredByUnitAsync(unit.Id, access, cancellationToken);
    }

    private async Task<ReconditioningItemDto> GetRequiredByUnitAsync(
        int productUnitId,
        ReconditioningAccessContext access,
        CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(cancellationToken);
        return BuildItems(context, access).First(item => item.ProductUnitId == productUnitId);
    }

    private async Task<ProductUnit> LoadUnitForDecisionAsync(int productUnitId, CancellationToken cancellationToken)
    {
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(item => item.Id == productUnitId, cancellationToken);
        return unit ?? throw new KeyNotFoundException($"Unidade de produto {productUnitId} não encontrada.");
    }

    private async Task<Nonconformity> ResolveRecoverableNonconformityAsync(
        int productUnitId,
        int? nonconformityId,
        CancellationToken cancellationToken)
    {
        var nonconformity = await ResolveNonconformityForRejectionAsync(productUnitId, nonconformityId, cancellationToken);
        if (!IsRecoverableSeverity(nonconformity.Severity))
        {
            throw new InvalidOperationException("A severidade da não conformidade não permite recuperação produtiva.");
        }

        return nonconformity;
    }

    private async Task<Nonconformity> ResolveNonconformityForRejectionAsync(
        int productUnitId,
        int? nonconformityId,
        CancellationToken cancellationToken)
    {
        IQueryable<Nonconformity> query = _db.Nonconformities.Where(item => item.ProductUnitId == productUnitId);
        if (nonconformityId.HasValue)
        {
            query = query.Where(item => item.Id == nonconformityId.Value);
        }

        var nonconformity = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return nonconformity
            ?? throw new InvalidOperationException("A unidade não tem uma não conformidade associada que justifique recuperação.");
    }

    private async Task EnsureCanRecoverAsync(
        ProductUnit unit,
        Nonconformity nonconformity,
        CancellationToken cancellationToken)
    {
        if (unit.IsReconditioned)
        {
            throw new InvalidOperationException("A unidade já se encontra marcada como recondicionada.");
        }

        if (unit.Status.Equals("Scrap", StringComparison.OrdinalIgnoreCase)
            || await _db.ScrapRecords.AnyAsync(item => item.ProductUnitId == unit.Id, cancellationToken))
        {
            throw new InvalidOperationException("Unidades em sucata não podem ser recondicionadas.");
        }

        if (!IsRecoverableSeverity(nonconformity.Severity))
        {
            throw new InvalidOperationException("A severidade crítica ou não recuperável não permite recondicionamento.");
        }

        if (unit.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            && unit.QualityStatus.Equals("PASS", StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(unit.RecoveryStatus)
                || unit.RecoveryStatus.Equals(ReconditioningStatuses.None, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Uma unidade concluída sem falha não pode ser marcada como recondicionada.");
        }
    }

    private async Task<ReworkRecord> ResolveOrCreateReworkAsync(
        int productUnitId,
        int nonconformityId,
        int? requestedReworkRecordId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        ReworkRecord? rework = null;
        if (requestedReworkRecordId.HasValue)
        {
            rework = await _db.ReworkRecords.FirstOrDefaultAsync(
                item => item.Id == requestedReworkRecordId.Value && item.ProductUnitId == productUnitId,
                cancellationToken);
        }

        rework ??= await ResolveLatestReworkAsync(productUnitId, nonconformityId, cancellationToken);
        if (rework is not null) return rework;

        rework = new ReworkRecord
        {
            ProductUnitId = productUnitId,
            NonconformityId = nonconformityId,
            StartedAt = now,
            Status = "Open",
            Notes = "Retrabalho criado automaticamente para recuperação produtiva."
        };
        _db.ReworkRecords.Add(rework);
        await _db.SaveChangesAsync(cancellationToken);
        return rework;
    }

    private async Task<ReworkRecord?> ResolveLatestReworkAsync(
        int productUnitId,
        int nonconformityId,
        CancellationToken cancellationToken)
    {
        return await _db.ReworkRecords
            .Where(item => item.ProductUnitId == productUnitId
                && (item.NonconformityId == nonconformityId || item.NonconformityId == null))
            .OrderByDescending(item => item.StartedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ReconditionRecord> ResolveOrCreateReconditioningRecordAsync(
        int productUnitId,
        int nonconformityId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var record = await _db.ReconditionRecords
            .Where(item => item.ProductUnitId == productUnitId && item.NonconformityId == nonconformityId)
            .OrderByDescending(item => item.RecordedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is not null) return record;

        record = new ReconditionRecord
        {
            ProductUnitId = productUnitId,
            NonconformityId = nonconformityId,
            Status = ReconditioningStatuses.Candidate,
            Decision = "Pending",
            RecordedAt = now
        };
        _db.ReconditionRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        return record;
    }

    private async Task RecordReconditioningEventAsync(
        string eventType,
        ProductUnit unit,
        Nonconformity nonconformity,
        ReworkRecord? rework,
        ReconditionRecord record,
        ReconditioningAccessContext access,
        DateTime occurredAt,
        string notes,
        CancellationToken cancellationToken)
    {
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"RECOND-{eventType}-{record.Id}-{occurredAt:yyyyMMddHHmmssfff}",
            EventType = eventType,
            ProductUnitId = unit.Id,
            ManufacturingOrderId = unit.ManufacturingOrderId,
            NonconformityId = nonconformity.Id,
            ReworkRecordId = rework?.Id,
            ReconditionRecordId = record.Id,
            ReasonCode = record.Decision,
            Severity = nonconformity.Severity,
            Source = OperationalEventSources.Api,
            PerformedByUserId = access.Username,
            OccurredAt = occurredAt,
            Notes = notes,
            Metadata = new Dictionary<string, object?>
            {
                ["unitCode"] = unit.UnitCode,
                ["recoveryStatus"] = unit.RecoveryStatus,
                ["qualityDisposition"] = unit.QualityDisposition,
                ["functionalValidation"] = record.FunctionalValidation,
                ["nextDisposition"] = record.NextDisposition
            }
        }, saveChanges: false, cancellationToken);
    }

    private async Task<ReconditioningDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var units = await _db.ProductUnits.AsNoTracking().OrderBy(item => item.UnitCode).ToListAsync(cancellationToken);
        var orders = await _db.ManufacturingOrders.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
        var sections = await _db.ProductionLineSections.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
        var lines = await _db.ProductionLines.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
        var nonconformities = await _db.Nonconformities.AsNoTracking().OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var reworks = await _db.ReworkRecords.AsNoTracking().OrderBy(item => item.StartedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var scraps = await _db.ScrapRecords.AsNoTracking().OrderBy(item => item.ScrappedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var records = await _db.ReconditionRecords.AsNoTracking().OrderBy(item => item.RecordedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return new ReconditioningDataContext(units, orders, sections, lines, nonconformities, reworks, scraps, records);
    }

    private IEnumerable<ReconditioningItemDto> BuildItems(
        ReconditioningDataContext context,
        ReconditioningAccessContext access)
    {
        if (access.Role.Equals(RoleNames.Customer, StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var nonconformitiesByUnit = context.Nonconformities.GroupBy(item => item.ProductUnitId).ToDictionary(group => group.Key, group => group.ToList());
        var recordsByUnit = context.ReconditionRecords.GroupBy(item => item.ProductUnitId).ToDictionary(group => group.Key, group => group.ToList());
        var reworksByUnit = context.ReworkRecords.GroupBy(item => item.ProductUnitId).ToDictionary(group => group.Key, group => group.ToList());
        var scrapsByUnit = context.ScrapRecords.GroupBy(item => item.ProductUnitId).ToDictionary(group => group.Key, group => group.ToList());

        foreach (var unit in context.ProductUnits)
        {
            if (!CanAccessUnit(unit, context, access)) continue;

            recordsByUnit.TryGetValue(unit.Id, out var unitRecords);
            nonconformitiesByUnit.TryGetValue(unit.Id, out var unitNonconformities);
            reworksByUnit.TryGetValue(unit.Id, out var unitReworks);
            scrapsByUnit.TryGetValue(unit.Id, out var unitScraps);

            var latestRecord = unitRecords?
                .OrderByDescending(item => item.RecordedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();

            var linkedNonconformity = latestRecord?.NonconformityId is null
                ? null
                : unitNonconformities?.FirstOrDefault(item => item.Id == latestRecord.NonconformityId.Value);

            var recoverableNonconformity = linkedNonconformity
                ?? unitNonconformities?
                    .Where(item => IsRecoverableSeverity(item.Severity))
                    .OrderByDescending(item => item.CreatedAt)
                    .ThenByDescending(item => item.Id)
                    .FirstOrDefault();

            var latestNonconformity = recoverableNonconformity
                ?? unitNonconformities?
                    .OrderByDescending(item => item.CreatedAt)
                    .ThenByDescending(item => item.Id)
                    .FirstOrDefault();

            var latestRework = latestRecord?.ReworkRecordId is null
                ? unitReworks?.OrderByDescending(item => item.StartedAt).ThenByDescending(item => item.Id).FirstOrDefault()
                : unitReworks?.FirstOrDefault(item => item.Id == latestRecord.ReworkRecordId.Value);

            var latestScrap = unitScraps?
                .OrderByDescending(item => item.ScrappedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();

            var hasRecoverySignal = latestRecord is not null
                || recoverableNonconformity is not null
                || unit.IsReconditioned
                || latestScrap is not null
                || !string.IsNullOrWhiteSpace(unit.RecoveryStatus) && !unit.RecoveryStatus.Equals(ReconditioningStatuses.None, StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(unit.QualityDisposition) && !unit.QualityDisposition.Equals(QualityDispositions.Normal, StringComparison.OrdinalIgnoreCase);

            if (!hasRecoverySignal) continue;

            var canMark = !unit.IsReconditioned
                && latestScrap is null
                && recoverableNonconformity is not null
                && (latestRecord is null
                    || latestRecord.Status.Equals(ReconditioningStatuses.Candidate, StringComparison.OrdinalIgnoreCase)
                    || latestRecord.Status.Equals(ReconditioningStatuses.Recoverable, StringComparison.OrdinalIgnoreCase));

            var canComplete = !unit.IsReconditioned
                && latestScrap is null
                && recoverableNonconformity is not null
                && (latestRecord is not null || latestRework is not null)
                && latestRecord?.Status.Equals(ReconditioningStatuses.Rejected, StringComparison.OrdinalIgnoreCase) != true;

            var canReject = !unit.IsReconditioned
                && latestScrap is null
                && latestNonconformity is not null
                && latestRecord?.Status.Equals(ReconditioningStatuses.Rejected, StringComparison.OrdinalIgnoreCase) != true;

            var section = unit.CurrentSectionId.HasValue && context.Sections.TryGetValue(unit.CurrentSectionId.Value, out var sectionValue)
                ? sectionValue
                : null;
            var line = section?.LineId is not null && context.Lines.TryGetValue(section.LineId.Value, out var lineValue)
                ? lineValue
                : null;
            var order = context.Orders.TryGetValue(unit.ManufacturingOrderId, out var orderValue) ? orderValue : null;

            yield return new ReconditioningItemDto(
                latestRecord?.Id,
                unit.Id,
                unit.UnitCode,
                unit.Status,
                unit.QualityStatus,
                unit.IsReconditioned,
                DisplayRecoveryStatus(unit, latestRecord, latestScrap),
                DisplayQualityDisposition(unit, latestScrap),
                unit.ReconditionedAt,
                unit.ReconditionReason,
                unit.ManufacturingOrderId,
                order?.OrderNumber,
                line?.LineCode,
                section?.SectionCode,
                latestNonconformity?.Id,
                latestNonconformity?.Severity,
                latestNonconformity?.Status,
                latestNonconformity?.Description,
                latestRework?.Id,
                latestRework?.Status,
                latestScrap?.Id,
                latestRecord?.Decision,
                latestRecord?.Reason,
                latestRecord?.Notes,
                latestRecord?.FunctionalValidation ?? false,
                latestRecord?.RecordedAt,
                latestRecord?.CompletedAt,
                latestRecord?.RejectedAt,
                latestRecord?.NextDisposition,
                canMark,
                canComplete,
                canReject);
        }
    }

    private static IEnumerable<ReconditioningItemDto> ApplyFilters(IEnumerable<ReconditioningItemDto> items, ReconditioningQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            items = items.Where(item => Matches(item.RecoveryStatus, query.Status) || Matches(item.Decision, query.Status));
        }

        if (!string.IsNullOrWhiteSpace(query.Line))
        {
            items = items.Where(item => Matches(item.CurrentLine, query.Line));
        }

        if (!string.IsNullOrWhiteSpace(query.Section))
        {
            items = items.Where(item => Matches(item.CurrentSection, query.Section));
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            items = items.Where(item => Matches(item.NonconformitySeverity, query.Severity));
        }

        if (!string.IsNullOrWhiteSpace(query.Quality))
        {
            items = items.Where(item => Matches(item.QualityStatus, query.Quality) || Matches(item.QualityDisposition, query.Quality));
        }

        return items;
    }

    private static ReconditioningSummaryDto BuildSummary(IReadOnlyCollection<ReconditioningItemDto> items)
    {
        var reconditioned = items.Count(item => item.IsReconditioned || Matches(item.RecoveryStatus, ReconditioningStatuses.Reconditioned));
        var rejected = items.Count(item => Matches(item.RecoveryStatus, ReconditioningStatuses.Rejected));
        var scrap = items.Count(item => item.ScrapRecordId.HasValue || Matches(item.QualityDisposition, QualityDispositions.Scrap));
        var candidates = items.Count(item => item.CanMarkRecoverable || IsCandidateStatus(item.RecoveryStatus));
        var inRecovery = items.Count(item => Matches(item.RecoveryStatus, ReconditioningStatuses.InRecovery));
        var closedDecisions = Math.Max(1, reconditioned + rejected + scrap);
        var recoveryRate = Math.Round(reconditioned * 100.0 / closedDecisions, 1);
        var recommendation = candidates > 0
            ? "Existem unidades elegíveis para recuperação produtiva. Rever prioridade com qualidade."
            : inRecovery > 0
                ? "Há unidades em recuperação. Validar conclusão funcional antes de fechar."
                : "Sem pendências críticas de recondicionamento.";

        return new ReconditioningSummaryDto(items.Count, candidates, inRecovery, reconditioned, rejected, scrap, recoveryRate, recommendation);
    }

    private static bool CanAccessUnit(
        ProductUnit unit,
        ReconditioningDataContext context,
        ReconditioningAccessContext access)
    {
        if (!access.Role.Equals(RoleNames.Operator, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(access.SectionCode) && string.IsNullOrWhiteSpace(access.LineCode))
        {
            return true;
        }

        if (!unit.CurrentSectionId.HasValue || !context.Sections.TryGetValue(unit.CurrentSectionId.Value, out var section))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(access.SectionCode)
            && section.SectionCode.Equals(access.SectionCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(access.LineCode) || !section.LineId.HasValue)
        {
            return false;
        }

        return context.Lines.TryGetValue(section.LineId.Value, out var line)
            && line.LineCode.Equals(access.LineCode, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecoverableSeverity(string? severity)
    {
        return !string.IsNullOrWhiteSpace(severity)
            && RecoverableSeverities.Contains(severity.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsCandidateStatus(string? value)
    {
        return Matches(value, ReconditioningStatuses.Candidate) || Matches(value, ReconditioningStatuses.Recoverable);
    }

    private static string DisplayRecoveryStatus(ProductUnit unit, ReconditionRecord? record, ScrapRecord? scrap)
    {
        if (unit.IsReconditioned) return ReconditioningStatuses.Reconditioned;
        if (scrap is not null) return ReconditioningStatuses.Rejected;
        if (!string.IsNullOrWhiteSpace(unit.RecoveryStatus) && !unit.RecoveryStatus.Equals(ReconditioningStatuses.None, StringComparison.OrdinalIgnoreCase))
        {
            return unit.RecoveryStatus;
        }

        return record?.Status ?? ReconditioningStatuses.Candidate;
    }

    private static string DisplayQualityDisposition(ProductUnit unit, ScrapRecord? scrap)
    {
        if (unit.IsReconditioned) return QualityDispositions.Reconditioned;
        if (scrap is not null) return QualityDispositions.Scrap;
        if (!string.IsNullOrWhiteSpace(unit.QualityDisposition)) return unit.QualityDisposition;
        if (unit.QualityStatus.Equals("FAIL", StringComparison.OrdinalIgnoreCase)) return QualityDispositions.Failed;
        if (unit.QualityStatus.Equals("PASS", StringComparison.OrdinalIgnoreCase)) return QualityDispositions.Normal;
        return QualityDispositions.Pending;
    }

    private static string NormalizeNextDisposition(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Indique a disposição seguinte após rejeição.");
        }

        var clean = value.Trim();
        if (clean.Equals("Scrap", StringComparison.OrdinalIgnoreCase)
            || clean.Equals("Sucata", StringComparison.OrdinalIgnoreCase))
        {
            return QualityDispositions.Scrap;
        }

        if (clean.Equals("Rework", StringComparison.OrdinalIgnoreCase)
            || clean.Equals("Retrabalho", StringComparison.OrdinalIgnoreCase)
            || clean.Equals("Recoverable", StringComparison.OrdinalIgnoreCase)
            || clean.Equals("Recuperável", StringComparison.OrdinalIgnoreCase))
        {
            return QualityDispositions.Recoverable;
        }

        if (clean.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || clean.Equals("Bloqueado", StringComparison.OrdinalIgnoreCase)
            || clean.Equals("Bloqueada", StringComparison.OrdinalIgnoreCase))
        {
            return QualityDispositions.Blocked;
        }

        throw new InvalidOperationException("Disposição seguinte inválida. Use Sucata, Retrabalho ou Bloqueado.");
    }

    private static string DisplayDisposition(string disposition)
    {
        return disposition switch
        {
            QualityDispositions.Scrap => "Sucata",
            QualityDispositions.Recoverable => "Retrabalho",
            QualityDispositions.Blocked => "Bloqueado",
            QualityDispositions.Reconditioned => "Recondicionado",
            _ => disposition
        };
    }

    private static bool Matches(string? value, string? expected)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !string.IsNullOrWhiteSpace(expected)
            && value.Trim().Equals(expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanRequired(string value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value.Trim();
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? MergeNotes(string? existing, string? addition)
    {
        if (string.IsNullOrWhiteSpace(addition)) return existing;
        if (string.IsNullOrWhiteSpace(existing)) return addition.Trim();
        return $"{existing.Trim()}\n{addition.Trim()}";
    }

    private sealed record ReconditioningDataContext(
        IReadOnlyList<ProductUnit> ProductUnits,
        IReadOnlyDictionary<int, ManufacturingOrder> Orders,
        IReadOnlyDictionary<int, ProductionLineSection> Sections,
        IReadOnlyDictionary<int, ProductionLine> Lines,
        IReadOnlyList<Nonconformity> Nonconformities,
        IReadOnlyList<ReworkRecord> ReworkRecords,
        IReadOnlyList<ScrapRecord> ScrapRecords,
        IReadOnlyList<ReconditionRecord> ReconditionRecords);
}
