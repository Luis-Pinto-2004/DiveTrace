namespace DriveTraceCore.Api.Models;

public sealed record ReconditioningSummaryDto(
    int Total,
    int Candidates,
    int InRecovery,
    int Reconditioned,
    int Rejected,
    int Scrap,
    double RecoveryRate,
    string Recommendation);

public sealed record ReconditioningListDto(
    ReconditioningSummaryDto Summary,
    IReadOnlyList<ReconditioningItemDto> Items);

public sealed record ReconditioningItemDto(
    int? Id,
    int ProductUnitId,
    string UnitCode,
    string UnitStatus,
    string QualityStatus,
    bool IsReconditioned,
    string RecoveryStatus,
    string QualityDisposition,
    DateTime? ReconditionedAt,
    string? ReconditionReason,
    int? ManufacturingOrderId,
    string? OrderNumber,
    string? CurrentLine,
    string? CurrentSection,
    int? NonconformityId,
    string? NonconformitySeverity,
    string? NonconformityStatus,
    string? NonconformityDescription,
    int? ReworkRecordId,
    string? ReworkStatus,
    int? ScrapRecordId,
    string? Decision,
    string? Reason,
    string? Notes,
    bool FunctionalValidation,
    DateTime? RecordedAt,
    DateTime? CompletedAt,
    DateTime? RejectedAt,
    string? NextDisposition,
    bool CanMarkRecoverable,
    bool CanComplete,
    bool CanReject);

public sealed record ReconditioningActionRequest(
    int? NonconformityId,
    int? ReworkRecordId,
    int? RecordedByResourceId,
    string Reason,
    string? Notes,
    bool FunctionalValidation);

public sealed record ReconditioningRejectRequest(
    int? NonconformityId,
    string Reason,
    string NextDisposition,
    string? Notes);

public sealed record ReconditioningQuery(
    string? Status,
    string? Line,
    string? Section,
    string? Severity,
    string? Quality);
