using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DriveTraceCore.Api.Models;

public sealed class SimulationRun : IEntity
{
    public int Id { get; set; }
    [MaxLength(80)] public string RunCode { get; set; } = string.Empty;
    [MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string ScenarioKey { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Draft";
    [MaxLength(20)] public string ExecutionMode { get; set; } = "Manual";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CurrentStep { get; set; }
    [MaxLength(120)] public string? CreatedByUserId { get; set; }
    public string? Notes { get; set; }
    [JsonIgnore] public ICollection<SimulationStep> Steps { get; set; } = new List<SimulationStep>();
}

public sealed class SimulationStep : IEntity
{
    public int Id { get; set; }
    public int SimulationRunId { get; set; }
    public int StepNumber { get; set; }
    [MaxLength(80)] public string StepType { get; set; } = string.Empty;
    [MaxLength(180)] public string Description { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public int? ProductUnitId { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public int? FromLineId { get; set; }
    public int? ToLineId { get; set; }
    public int? FromSectionId { get; set; }
    public int? ToSectionId { get; set; }
    [MaxLength(40)] public string? Result { get; set; }
    public int? OperationalEventId { get; set; }
    [JsonIgnore] public SimulationRun? SimulationRun { get; set; }
}
