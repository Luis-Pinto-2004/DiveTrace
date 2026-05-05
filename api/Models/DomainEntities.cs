using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DriveTraceCore.Api.Models;

public interface IEntity
{
    int Id { get; set; }
}

public sealed class Product : IEntity
{
    public int Id { get; set; }
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    public string? Info { get; set; }
    [JsonIgnore] public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    [JsonIgnore] public ICollection<ManufacturingProcess> ManufacturingProcesses { get; set; } = new List<ManufacturingProcess>();
}

public sealed class Variant : IEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    [MaxLength(80)] public string VariantCode { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [JsonIgnore] public Product? Product { get; set; }
}

public sealed class ManufacturingOrder : IEntity
{
    public int Id { get; set; }
    [MaxLength(80)] public string OrderNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int ManufacturingProcessId { get; set; }
    public int ProductionLineId { get; set; }
    public int PlannedQty { get; set; }
    public DateTime ScheduledUntil { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Planned";
    public string? Observations { get; set; }
    [JsonIgnore] public Product? Product { get; set; }
    [JsonIgnore] public Variant? Variant { get; set; }
    [JsonIgnore] public ManufacturingProcess? ManufacturingProcess { get; set; }
    [JsonIgnore] public ProductionLine? ProductionLine { get; set; }
    [JsonIgnore] public ICollection<ProductUnit> ProductUnits { get; set; } = new List<ProductUnit>();
}

public sealed class ProductionLine : IEntity
{
    public int Id { get; set; }
    [MaxLength(80)] public string LineCode { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [JsonIgnore] public ICollection<ProductionLineSection> Sections { get; set; } = new List<ProductionLineSection>();
}

public sealed class ProductionLineSection : IEntity
{
    public int Id { get; set; }
    [MaxLength(80)] public string SectionCode { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string SectionType { get; set; } = string.Empty;
    public int? LineId { get; set; }
    [JsonIgnore] public ProductionLine? Line { get; set; }
    [JsonIgnore] public ICollection<Checkpoint> Checkpoints { get; set; } = new List<Checkpoint>();
    [JsonIgnore] public ICollection<SupportLocalizationHistory> SupportLocalizationHistory { get; set; } = new List<SupportLocalizationHistory>();
}

public sealed class Resource : IEntity
{
    public int Id { get; set; }
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [MaxLength(80)] public string Type { get; set; } = string.Empty;
    [MaxLength(180)] public string Function { get; set; } = string.Empty;
}

public sealed class ManufacturingProcess : IEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    [MaxLength(160)] public string ProcessName { get; set; } = string.Empty;
    public string? Info { get; set; }
    [JsonIgnore] public Product? Product { get; set; }
    [JsonIgnore] public ICollection<ManufacturingProcessPhase> ProcessPhases { get; set; } = new List<ManufacturingProcessPhase>();
}

public sealed class ManufacturingSectionPhase : IEntity
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    [MaxLength(180)] public string PhaseInfo { get; set; } = string.Empty;
    public int PhaseDuration { get; set; }
    [JsonIgnore] public ProductionLineSection? Section { get; set; }
}

public sealed class ManufacturingProcessPhase : IEntity
{
    public int Id { get; set; }
    public int ManufacturingProcessId { get; set; }
    public int ManufacturingPhaseId { get; set; }
    public int? ResourceId { get; set; }
    public int NumberStepOrder { get; set; }
    [JsonIgnore] public ManufacturingProcess? ManufacturingProcess { get; set; }
    [JsonIgnore] public ManufacturingSectionPhase? ManufacturingPhase { get; set; }
    [JsonIgnore] public Resource? Resource { get; set; }
}

public sealed class Checkpoint : IEntity
{
    public int Id { get; set; }
    [MaxLength(80)] public string CheckpointCode { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Active";
    public int SectionId { get; set; }
    [JsonIgnore] public ProductionLineSection? Section { get; set; }
}

public sealed class ProductUnit : IEntity
{
    public int Id { get; set; }
    public int ManufacturingOrderId { get; set; }
    public int? VariantId { get; set; }
    public int? ParentUnitId { get; set; }
    [MaxLength(80)] public string UnitCode { get; set; } = string.Empty;
    [MaxLength(60)] public string UnitType { get; set; } = "Subproduct";
    [MaxLength(40)] public string Status { get; set; } = "Active";
    public int? CurrentSupportId { get; set; }
    public int? CurrentSectionId { get; set; }
    [MaxLength(40)] public string QualityStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    [JsonIgnore] public ManufacturingOrder? ManufacturingOrder { get; set; }
    [JsonIgnore] public Variant? Variant { get; set; }
    [JsonIgnore] public ProductUnit? ParentUnit { get; set; }
    [JsonIgnore] public Support? CurrentSupport { get; set; }
    [JsonIgnore] public ProductionLineSection? CurrentSection { get; set; }
    [JsonIgnore] public ICollection<ProductUnit> ChildUnits { get; set; } = new List<ProductUnit>();
}

public sealed class Support : IEntity
{
    public int Id { get; set; }
    [MaxLength(80)] public string SupportCode { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Available";
    public int? CurrentSectionId { get; set; }
    [JsonIgnore] public ProductionLineSection? CurrentSection { get; set; }
}

public sealed class UnitSupportAssignment : IEntity
{
    public int Id { get; set; }
    public int ProductUnitId { get; set; }
    public int SupportId { get; set; }
    public DateTime DateTimeIn { get; set; } = DateTime.UtcNow;
    public DateTime? DateTimeOut { get; set; }
    [JsonIgnore] public ProductUnit? ProductUnit { get; set; }
    [JsonIgnore] public Support? Support { get; set; }
}

public sealed class SupportLocalizationHistory : IEntity
{
    public int Id { get; set; }
    public int SupportId { get; set; }
    public int SectionId { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    [MaxLength(80)] public string EventType { get; set; } = "Movement";
    [JsonIgnore] public Support? Support { get; set; }
    [JsonIgnore] public ProductionLineSection? Section { get; set; }
}

public sealed class Rack : IEntity
{
    public int Id { get; set; }
    public int? SectionId { get; set; }
    [MaxLength(80)] public string RackCode { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = "Available";
    [JsonIgnore] public ProductionLineSection? Section { get; set; }
}

public sealed class RackSupportAssignment : IEntity
{
    public int Id { get; set; }
    public int RackId { get; set; }
    public int SupportId { get; set; }
    public DateTime DateTimeIn { get; set; } = DateTime.UtcNow;
    public DateTime? DateTimeOut { get; set; }
    [JsonIgnore] public Rack? Rack { get; set; }
    [JsonIgnore] public Support? Support { get; set; }
}

public sealed class RawMaterial : IEntity
{
    public int Id { get; set; }
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    public string? Info { get; set; }
    [JsonIgnore] public ICollection<LotRawMaterial> Lots { get; set; } = new List<LotRawMaterial>();
}

public sealed class LotRawMaterial : IEntity
{
    public int Id { get; set; }
    public int RawMaterialId { get; set; }
    public int? SectionId { get; set; }
    [MaxLength(80)] public string LotNumber { get; set; } = string.Empty;
    public int LotQuantity { get; set; }
    [MaxLength(40)] public string LotUnit { get; set; } = string.Empty;
    [JsonIgnore] public RawMaterial? RawMaterial { get; set; }
    [JsonIgnore] public ProductionLineSection? Section { get; set; }
}

public sealed class UnitMaterialLotUsage : IEntity
{
    public int Id { get; set; }
    public int ProductUnitId { get; set; }
    public int LotId { get; set; }
    [MaxLength(80)] public string AssociationType { get; set; } = "Consumed";
    public int Quantity { get; set; }
    [JsonIgnore] public ProductUnit? ProductUnit { get; set; }
    [JsonIgnore] public LotRawMaterial? Lot { get; set; }
}

public sealed class QualityResult : IEntity
{
    public int Id { get; set; }
    public int ProductUnitId { get; set; }
    public int? CheckpointId { get; set; }
    [MaxLength(20)] public string Result { get; set; } = "PASS";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    [JsonIgnore] public ProductUnit? ProductUnit { get; set; }
    [JsonIgnore] public Checkpoint? Checkpoint { get; set; }
}

public sealed class Nonconformity : IEntity
{
    public int Id { get; set; }
    public int ProductUnitId { get; set; }
    public int? QualityResultId { get; set; }
    [MaxLength(40)] public string Severity { get; set; } = "Minor";
    [MaxLength(40)] public string Status { get; set; } = "Open";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonIgnore] public ProductUnit? ProductUnit { get; set; }
    [JsonIgnore] public QualityResult? QualityResult { get; set; }
}

public sealed class ReworkRecord : IEntity
{
    public int Id { get; set; }
    public int ProductUnitId { get; set; }
    public int? NonconformityId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    [MaxLength(40)] public string Status { get; set; } = "Open";
    public string? Notes { get; set; }
    [JsonIgnore] public ProductUnit? ProductUnit { get; set; }
    [JsonIgnore] public Nonconformity? Nonconformity { get; set; }
}

public sealed class ScrapRecord : IEntity
{
    public int Id { get; set; }
    public int ProductUnitId { get; set; }
    public int? NonconformityId { get; set; }
    public DateTime ScrappedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    [JsonIgnore] public ProductUnit? ProductUnit { get; set; }
    [JsonIgnore] public Nonconformity? Nonconformity { get; set; }
}

public sealed class Prediction : IEntity
{
    public int Id { get; set; }
    public int? ManufacturingOrderId { get; set; }
    public byte[]? Model { get; set; }
    [MaxLength(80)] public string ModelVersion { get; set; } = "future-v1";
    [MaxLength(80)] public string ModelType { get; set; } = "Placeholder";
    public DateTime? LastDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonIgnore] public ManufacturingOrder? ManufacturingOrder { get; set; }
}

public sealed class ManualEventRequest
{
    public string EventType { get; set; } = "MoveSupport";
    public string? ProductUnitCode { get; set; }
    public string? SupportCode { get; set; }
    public string? SectionCode { get; set; }
    public string? Result { get; set; }
    public string? Notes { get; set; }
}

public sealed class PlaybackRequest
{
    public string Scenario { get; set; } = "door-line-demo";
}
