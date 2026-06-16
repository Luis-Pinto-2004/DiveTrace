namespace DriveTraceCore.Api.Models;

public sealed class SimulationScenarioDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int StepCount { get; init; }
    public IReadOnlyList<string> Entities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public string Outcome { get; init; } = string.Empty;
    public string DefaultUnitCode { get; init; } = string.Empty;
    public string DefaultOrderNumber { get; init; } = string.Empty;
}

public sealed class SimulationRunCreateRequest
{
    public string ScenarioKey { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Speed { get; set; } = "Manual";
    public string? Notes { get; set; }
}

public class SimulationRunSummaryDto
{
    public int Id { get; init; }
    public string RunCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ScenarioKey { get; init; } = string.Empty;
    public string ScenarioName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ExecutionMode { get; init; } = string.Empty;
    public int CurrentStep { get; init; }
    public int StepCount { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? PausedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? LastExecutedAt { get; init; }
    public string? LastStepType { get; init; }
    public string? LastStepDescription { get; init; }
    public string? LastResult { get; init; }
    public int? ProductUnitId { get; init; }
    public string? UnitCode { get; init; }
    public int? ManufacturingOrderId { get; init; }
    public string? OrderNumber { get; init; }
    public string? CurrentLine { get; init; }
    public string? CurrentSection { get; init; }
    public string? SupportCode { get; init; }
    public string? QualityStatus { get; init; }
    public string? QualityDisposition { get; init; }
    public string? RecoveryStatus { get; init; }
    public bool IsReconditioned { get; init; }
}

public sealed class SimulationStepDto
{
    public int Id { get; init; }
    public int StepNumber { get; init; }
    public string StepType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; }
    public int? ProductUnitId { get; init; }
    public int? ManufacturingOrderId { get; init; }
    public int? FromLineId { get; init; }
    public int? ToLineId { get; init; }
    public int? FromSectionId { get; init; }
    public int? ToSectionId { get; init; }
    public string? Result { get; init; }
    public int? OperationalEventId { get; init; }
}

public sealed class SimulationRunDetailDto : SimulationRunSummaryDto
{
    public string? Notes { get; init; }
    public IReadOnlyList<SimulationStepDto> Steps { get; init; } = Array.Empty<SimulationStepDto>();
}

public sealed class SimulationStateDto
{
    public DateTime GeneratedAt { get; init; }
    public string LastAction { get; init; } = string.Empty;
    public int UnitsInMotion { get; init; }
    public IReadOnlyList<string> Alerts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<SimulationScenarioDto> Scenarios { get; init; } = Array.Empty<SimulationScenarioDto>();
    public IReadOnlyList<SimulationRunSummaryDto> ActiveRuns { get; init; } = Array.Empty<SimulationRunSummaryDto>();
    public IReadOnlyList<SimulationRunSummaryDto> RecentRuns { get; init; } = Array.Empty<SimulationRunSummaryDto>();
}
