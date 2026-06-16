namespace DriveTraceCore.Api.Models;

public sealed record TraceGraphDto(
    string Scope,
    string Title,
    TraceGraphSummaryDto Summary,
    IReadOnlyList<TraceGraphNodeDto> Nodes,
    IReadOnlyList<TraceGraphEdgeDto> Edges,
    IReadOnlyList<TraceGraphLegendItemDto> Legend,
    IReadOnlyList<TraceGraphWarningDto> Warnings);

public sealed record TraceGraphSummaryDto(
    string Status,
    string? CurrentLine,
    string? CurrentSection,
    string? QualityStatus,
    bool HasOpenIssues,
    IReadOnlyDictionary<string, object?> Metrics,
    string Recommendation);

public sealed record TraceGraphNodeDto(
    string Id,
    string Type,
    string Label,
    string? Subtitle,
    string? Status,
    string Severity,
    string? Group,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record TraceGraphEdgeDto(
    string Id,
    string Source,
    string Target,
    string Type,
    string Label,
    DateTime? Timestamp,
    string Severity,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record TraceGraphLegendItemDto(
    string Type,
    string Label,
    string Color,
    string Description);

public sealed record TraceGraphWarningDto(
    string Code,
    string Message,
    string Severity);

public sealed record TraceGraphOptionsDto(
    IReadOnlyList<TraceGraphOptionDto> ProductUnits,
    IReadOnlyList<TraceGraphOptionDto> ManufacturingOrders,
    IReadOnlyList<TraceGraphOptionDto> Lines,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> QualityStatuses,
    IReadOnlyList<string> NodeTypes,
    IReadOnlyList<string> SupportedModes);

public sealed record TraceGraphOptionDto(
    int Id,
    string Code,
    string Label,
    string? Status,
    string? Group);
