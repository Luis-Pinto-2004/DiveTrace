using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Data;

public sealed class DriveTraceDbContext : DbContext
{
    public DriveTraceDbContext(DbContextOptions<DriveTraceDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ManufacturingOrder> ManufacturingOrders => Set<ManufacturingOrder>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<ProductionLineSection> ProductionLineSections => Set<ProductionLineSection>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ManufacturingProcess> ManufacturingProcesses => Set<ManufacturingProcess>();
    public DbSet<ManufacturingSectionPhase> ManufacturingSectionPhases => Set<ManufacturingSectionPhase>();
    public DbSet<ManufacturingProcessPhase> ManufacturingProcessPhases => Set<ManufacturingProcessPhase>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<ProductUnitLocationHistory> ProductUnitLocationHistory => Set<ProductUnitLocationHistory>();
    public DbSet<OperationalEvent> OperationalEvents => Set<OperationalEvent>();
    public DbSet<Support> Supports => Set<Support>();
    public DbSet<UnitSupportAssignment> UnitSupportAssignments => Set<UnitSupportAssignment>();
    public DbSet<SupportLocalizationHistory> SupportLocalizationHistory => Set<SupportLocalizationHistory>();
    public DbSet<Rack> Racks => Set<Rack>();
    public DbSet<RackSupportAssignment> RackSupportAssignments => Set<RackSupportAssignment>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<LotRawMaterial> LotRawMaterials => Set<LotRawMaterial>();
    public DbSet<UnitMaterialLotUsage> UnitMaterialLotUsages => Set<UnitMaterialLotUsage>();
    public DbSet<QualityResult> QualityResults => Set<QualityResult>();
    public DbSet<Nonconformity> Nonconformities => Set<Nonconformity>();
    public DbSet<ReworkRecord> ReworkRecords => Set<ReworkRecord>();
    public DbSet<ScrapRecord> ScrapRecords => Set<ScrapRecord>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Variant>().HasIndex(x => new { x.ProductId, x.VariantCode }).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        modelBuilder.Entity<ManufacturingOrder>().HasIndex(x => x.OrderNumber).IsUnique();
        modelBuilder.Entity<ManufacturingOrder>().HasIndex(x => x.PublicTrackingCode).IsUnique();
        modelBuilder.Entity<ProductionLine>().HasIndex(x => x.LineCode).IsUnique();
        modelBuilder.Entity<ProductionLineSection>().HasIndex(x => x.SectionCode).IsUnique();
        modelBuilder.Entity<Checkpoint>().HasIndex(x => x.CheckpointCode).IsUnique();
        modelBuilder.Entity<ProductUnit>().HasIndex(x => x.UnitCode).IsUnique();
        modelBuilder.Entity<Support>().HasIndex(x => x.SupportCode).IsUnique();
        modelBuilder.Entity<Rack>().HasIndex(x => x.RackCode).IsUnique();
        modelBuilder.Entity<RawMaterial>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<LotRawMaterial>().HasIndex(x => x.LotNumber).IsUnique();

        modelBuilder.Entity<ManufacturingOrder>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.ManufacturingOrders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductUnit>()
            .HasOne(x => x.ParentUnit)
            .WithMany(x => x.ChildUnits)
            .HasForeignKey(x => x.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnit>()
            .HasOne(x => x.CurrentSupport)
            .WithMany()
            .HasForeignKey(x => x.CurrentSupportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnit>()
            .HasOne(x => x.CurrentSection)
            .WithMany()
            .HasForeignKey(x => x.CurrentSectionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasIndex(x => new { x.ProductUnitId, x.OccurredAt });

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasIndex(x => new { x.ToProductionLineId, x.OccurredAt });

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.ProductUnit)
            .WithMany(x => x.LocationHistory)
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.FromProductionLine)
            .WithMany()
            .HasForeignKey(x => x.FromProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.ToProductionLine)
            .WithMany()
            .HasForeignKey(x => x.ToProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.FromSection)
            .WithMany()
            .HasForeignKey(x => x.FromSectionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.ToSection)
            .WithMany()
            .HasForeignKey(x => x.ToSectionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.FromSupport)
            .WithMany()
            .HasForeignKey(x => x.FromSupportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnitLocationHistory>()
            .HasOne(x => x.ToSupport)
            .WithMany()
            .HasForeignKey(x => x.ToSupportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OperationalEvent>()
            .HasIndex(x => x.EventCode)
            .IsUnique();

        modelBuilder.Entity<OperationalEvent>()
            .HasIndex(x => new { x.OccurredAt, x.EventType });

        modelBuilder.Entity<OperationalEvent>()
            .HasIndex(x => new { x.ProductUnitId, x.OccurredAt });

        modelBuilder.Entity<OperationalEvent>()
            .HasIndex(x => new { x.SupportId, x.OccurredAt });

        modelBuilder.Entity<OperationalEvent>()
            .HasIndex(x => new { x.ManufacturingOrderId, x.OccurredAt });

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.Support)
            .WithMany()
            .HasForeignKey(x => x.SupportId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.ManufacturingOrder)
            .WithMany()
            .HasForeignKey(x => x.ManufacturingOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.FromProductionLine)
            .WithMany()
            .HasForeignKey(x => x.FromProductionLineId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.ToProductionLine)
            .WithMany()
            .HasForeignKey(x => x.ToProductionLineId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.FromSection)
            .WithMany()
            .HasForeignKey(x => x.FromSectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.ToSection)
            .WithMany()
            .HasForeignKey(x => x.ToSectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.Checkpoint)
            .WithMany()
            .HasForeignKey(x => x.CheckpointId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.QualityResult)
            .WithMany()
            .HasForeignKey(x => x.QualityResultId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.Nonconformity)
            .WithMany()
            .HasForeignKey(x => x.NonconformityId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.ReworkRecord)
            .WithMany()
            .HasForeignKey(x => x.ReworkRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.ScrapRecord)
            .WithMany()
            .HasForeignKey(x => x.ScrapRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OperationalEvent>()
            .HasOne(x => x.Rack)
            .WithMany()
            .HasForeignKey(x => x.RackId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Support>()
            .HasOne(x => x.CurrentSection)
            .WithMany()
            .HasForeignKey(x => x.CurrentSectionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UnitSupportAssignment>()
            .HasIndex(x => new { x.ProductUnitId, x.DateTimeOut });

        modelBuilder.Entity<UnitSupportAssignment>()
            .HasIndex(x => new { x.SupportId, x.DateTimeOut });

        modelBuilder.Entity<RackSupportAssignment>()
            .HasIndex(x => new { x.RackId, x.SupportId, x.DateTimeOut });

        modelBuilder.Entity<QualityResult>()
            .Property(x => x.Result)
            .HasMaxLength(20);
    }
}
