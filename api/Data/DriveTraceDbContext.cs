using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Data;

public sealed class DriveTraceDbContext : DbContext
{
    public DriveTraceDbContext(DbContextOptions<DriveTraceDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<ManufacturingOrder> ManufacturingOrders => Set<ManufacturingOrder>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<ProductionLineSection> ProductionLineSections => Set<ProductionLineSection>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ManufacturingProcess> ManufacturingProcesses => Set<ManufacturingProcess>();
    public DbSet<ManufacturingSectionPhase> ManufacturingSectionPhases => Set<ManufacturingSectionPhase>();
    public DbSet<ManufacturingProcessPhase> ManufacturingProcessPhases => Set<ManufacturingProcessPhase>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
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
        modelBuilder.Entity<ManufacturingOrder>().HasIndex(x => x.OrderNumber).IsUnique();
        modelBuilder.Entity<ProductionLine>().HasIndex(x => x.LineCode).IsUnique();
        modelBuilder.Entity<ProductionLineSection>().HasIndex(x => x.SectionCode).IsUnique();
        modelBuilder.Entity<Checkpoint>().HasIndex(x => x.CheckpointCode).IsUnique();
        modelBuilder.Entity<ProductUnit>().HasIndex(x => x.UnitCode).IsUnique();
        modelBuilder.Entity<Support>().HasIndex(x => x.SupportCode).IsUnique();
        modelBuilder.Entity<Rack>().HasIndex(x => x.RackCode).IsUnique();
        modelBuilder.Entity<RawMaterial>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<LotRawMaterial>().HasIndex(x => x.LotNumber).IsUnique();

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
