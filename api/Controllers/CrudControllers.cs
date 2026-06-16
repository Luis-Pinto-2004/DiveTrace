using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resource = DriveTraceCore.Api.Models.Resource;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
public abstract class CrudController<TEntity> : ControllerBase where TEntity : class, IEntity
{
    protected readonly DriveTraceDbContext Db;
    protected virtual string? ReadPermission => null;
    protected virtual string WritePermission => PermissionNames.MasterDataManage;

    protected CrudController(DriveTraceDbContext db)
    {
        Db = db;
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TEntity>>> GetAll()
    {
        if (!CanRead())
        {
            return PermissionCatalogService.Forbidden(ReadPermission!);
        }

        return Ok(await Db.Set<TEntity>().AsNoTracking().OrderBy(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public virtual async Task<ActionResult<TEntity>> GetById(int id)
    {
        if (!CanRead())
        {
            return PermissionCatalogService.Forbidden(ReadPermission!);
        }

        var entity = await Db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TEntity>> Create(TEntity entity)
    {
        if (!CanWrite())
        {
            return PermissionCatalogService.Forbidden(WritePermission);
        }

        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<IActionResult> Update(int id, TEntity entity)
    {
        if (!CanWrite())
        {
            return PermissionCatalogService.Forbidden(WritePermission);
        }

        if (id != entity.Id)
        {
            return BadRequest("The route id does not match the entity id.");
        }

        var exists = await Db.Set<TEntity>().AnyAsync(x => x.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        Db.Entry(entity).State = EntityState.Modified;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        if (!CanWrite())
        {
            return PermissionCatalogService.Forbidden(WritePermission);
        }

        var entity = await Db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        Db.Set<TEntity>().Remove(entity);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private bool CanRead()
    {
        return ReadPermission is null || PermissionCatalogService.HasPermission(HttpContext, ReadPermission);
    }

    private bool CanWrite()
    {
        return PermissionCatalogService.HasPermission(HttpContext, WritePermission);
    }
}

[Route("api/products")]
public sealed class ProductsController : CrudController<Product>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ProductsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/variants")]
public sealed class VariantsController : CrudController<Variant>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public VariantsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/customers")]
public sealed class CustomersController : CrudController<Customer>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public CustomersController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-orders")]
public sealed class ManufacturingOrdersController : CrudController<ManufacturingOrder>
{
    protected override string? ReadPermission => PermissionNames.OrdersView;
    protected override string WritePermission => PermissionNames.OrdersManage;
    public ManufacturingOrdersController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/production-lines")]
public sealed class ProductionLinesController : CrudController<ProductionLine>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ProductionLinesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/production-line-sections")]
public sealed class ProductionLineSectionsController : CrudController<ProductionLineSection>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ProductionLineSectionsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/resources")]
public sealed class ResourcesController : CrudController<Resource>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ResourcesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-processes")]
public sealed class ManufacturingProcessesController : CrudController<ManufacturingProcess>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ManufacturingProcessesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-section-phases")]
public sealed class ManufacturingSectionPhasesController : CrudController<ManufacturingSectionPhase>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ManufacturingSectionPhasesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-process-phases")]
public sealed class ManufacturingProcessPhasesController : CrudController<ManufacturingProcessPhase>
{
    protected override string? ReadPermission => PermissionNames.MasterDataManage;
    public ManufacturingProcessPhasesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/checkpoints")]
public sealed class CheckpointsController : CrudController<Checkpoint>
{
    protected override string? ReadPermission => PermissionNames.QualityView;
    protected override string WritePermission => PermissionNames.MasterDataManage;
    public CheckpointsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/product-units")]
public sealed class ProductUnitsController : CrudController<ProductUnit>
{
    protected override string? ReadPermission => PermissionNames.ProductUnitsView;
    public ProductUnitsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/product-unit-location-history")]
public sealed class ProductUnitLocationHistoryController : CrudController<ProductUnitLocationHistory>
{
    protected override string? ReadPermission => PermissionNames.ProductUnitsTrace;
    protected override string WritePermission => PermissionNames.ProductUnitsTransfer;
    public ProductUnitLocationHistoryController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/supports")]
public sealed class SupportsController : CrudController<Support>
{
    protected override string? ReadPermission => PermissionNames.ProductUnitsView;
    protected override string WritePermission => PermissionNames.SupportsManage;
    public SupportsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/unit-support-assignments")]
public sealed class UnitSupportAssignmentsController : CrudController<UnitSupportAssignment>
{
    protected override string? ReadPermission => PermissionNames.ProductUnitsTrace;
    protected override string WritePermission => PermissionNames.SupportsManage;
    public UnitSupportAssignmentsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/support-localization-history")]
public sealed class SupportLocalizationHistoryController : CrudController<SupportLocalizationHistory>
{
    protected override string? ReadPermission => PermissionNames.ProductUnitsTrace;
    protected override string WritePermission => PermissionNames.ProductUnitsTransfer;
    public SupportLocalizationHistoryController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/racks")]
public sealed class RacksController : CrudController<Rack>
{
    protected override string? ReadPermission => PermissionNames.RacksView;
    protected override string WritePermission => PermissionNames.RacksManage;
    public RacksController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/rack-support-assignments")]
public sealed class RackSupportAssignmentsController : CrudController<RackSupportAssignment>
{
    protected override string? ReadPermission => PermissionNames.RacksView;
    protected override string WritePermission => PermissionNames.RacksManage;
    private readonly OperationalEventService _events;

    public RackSupportAssignmentsController(DriveTraceDbContext db, OperationalEventService events) : base(db)
    {
        _events = events;
    }

    [HttpPost]
    public override async Task<ActionResult<RackSupportAssignment>> Create(RackSupportAssignment entity)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.RacksManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.RacksManage);
        }

        Db.RackSupportAssignments.Add(entity);
        await Db.SaveChangesAsync();

        var unit = await Db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.CurrentSupportId == entity.SupportId);
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"RACK-ASSIGNMENT-{entity.Id}",
            EventType = OperationalEventTypes.RackAssigned,
            ProductUnitId = unit?.Id,
            SupportId = entity.SupportId,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            RackId = entity.RackId,
            Source = OperationalEventSources.Api,
            OccurredAt = entity.DateTimeIn,
            Notes = "Suporte associado a rack por operação API."
        });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }
}

[Route("api/raw-materials")]
public sealed class RawMaterialsController : CrudController<RawMaterial>
{
    protected override string? ReadPermission => PermissionNames.MaterialsView;
    protected override string WritePermission => PermissionNames.MaterialsManage;
    public RawMaterialsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/lot-raw-materials")]
public sealed class LotRawMaterialsController : CrudController<LotRawMaterial>
{
    protected override string? ReadPermission => PermissionNames.MaterialsView;
    protected override string WritePermission => PermissionNames.MaterialsManage;
    public LotRawMaterialsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/unit-material-lot-usages")]
public sealed class UnitMaterialLotUsagesController : CrudController<UnitMaterialLotUsage>
{
    protected override string? ReadPermission => PermissionNames.MaterialsView;
    protected override string WritePermission => PermissionNames.MaterialsManage;
    public UnitMaterialLotUsagesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/quality-results")]
public sealed class QualityResultsController : CrudController<QualityResult>
{
    protected override string? ReadPermission => PermissionNames.QualityView;
    protected override string WritePermission => PermissionNames.QualityRecord;
    private readonly OperationalEventService _events;

    public QualityResultsController(DriveTraceDbContext db, OperationalEventService events) : base(db)
    {
        _events = events;
    }

    [HttpPost]
    public override async Task<ActionResult<QualityResult>> Create(QualityResult entity)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.QualityRecord))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.QualityRecord);
        }

        Db.QualityResults.Add(entity);
        await Db.SaveChangesAsync();

        var unit = await Db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.ProductUnitId);
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"QUALITY-{entity.Id}",
            EventType = OperationalEventTypes.QualityRecorded,
            ProductUnitId = entity.ProductUnitId,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            CheckpointId = entity.CheckpointId,
            QualityResultId = entity.Id,
            ReasonCode = entity.Result,
            Severity = entity.Result.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "Maior" : null,
            Source = OperationalEventSources.Api,
            OccurredAt = entity.RecordedAt,
            Notes = entity.Notes
        });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }
}

[Route("api/nonconformities")]
public sealed class NonconformitiesController : CrudController<Nonconformity>
{
    protected override string? ReadPermission => PermissionNames.QualityView;
    protected override string WritePermission => PermissionNames.QualityDecide;
    private readonly OperationalEventService _events;

    public NonconformitiesController(DriveTraceDbContext db, OperationalEventService events) : base(db)
    {
        _events = events;
    }

    [HttpPost]
    public override async Task<ActionResult<Nonconformity>> Create(Nonconformity entity)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.QualityDecide))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.QualityDecide);
        }

        Db.Nonconformities.Add(entity);
        await Db.SaveChangesAsync();

        var unit = await Db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.ProductUnitId);
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"NC-{entity.Id}",
            EventType = OperationalEventTypes.NonconformityOpened,
            ProductUnitId = entity.ProductUnitId,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            QualityResultId = entity.QualityResultId,
            NonconformityId = entity.Id,
            Severity = entity.Severity,
            Source = OperationalEventSources.Api,
            OccurredAt = entity.CreatedAt,
            Notes = entity.Description
        });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }
}

[Route("api/rework-records")]
public sealed class ReworkRecordsController : CrudController<ReworkRecord>
{
    protected override string? ReadPermission => PermissionNames.QualityView;
    protected override string WritePermission => PermissionNames.QualityDecide;
    private readonly OperationalEventService _events;

    public ReworkRecordsController(DriveTraceDbContext db, OperationalEventService events) : base(db)
    {
        _events = events;
    }

    [HttpPost]
    public override async Task<ActionResult<ReworkRecord>> Create(ReworkRecord entity)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.QualityDecide))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.QualityDecide);
        }

        Db.ReworkRecords.Add(entity);
        await Db.SaveChangesAsync();

        var unit = await Db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.ProductUnitId);
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"REWORK-{entity.Id}",
            EventType = entity.EndedAt.HasValue ? OperationalEventTypes.ReworkCompleted : OperationalEventTypes.ReworkStarted,
            ProductUnitId = entity.ProductUnitId,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            NonconformityId = entity.NonconformityId,
            ReworkRecordId = entity.Id,
            Source = OperationalEventSources.Api,
            OccurredAt = entity.EndedAt ?? entity.StartedAt,
            Notes = entity.Notes
        });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }
}

[Route("api/scrap-records")]
public sealed class ScrapRecordsController : CrudController<ScrapRecord>
{
    protected override string? ReadPermission => PermissionNames.QualityView;
    protected override string WritePermission => PermissionNames.QualityDecide;
    private readonly OperationalEventService _events;

    public ScrapRecordsController(DriveTraceDbContext db, OperationalEventService events) : base(db)
    {
        _events = events;
    }

    [HttpPost]
    public override async Task<ActionResult<ScrapRecord>> Create(ScrapRecord entity)
    {
        if (!PermissionCatalogService.HasPermission(HttpContext, PermissionNames.QualityDecide))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.QualityDecide);
        }

        Db.ScrapRecords.Add(entity);
        await Db.SaveChangesAsync();

        var unit = await Db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.ProductUnitId);
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SCRAP-{entity.Id}",
            EventType = OperationalEventTypes.ScrapRecorded,
            ProductUnitId = entity.ProductUnitId,
            ManufacturingOrderId = unit?.ManufacturingOrderId,
            NonconformityId = entity.NonconformityId,
            ScrapRecordId = entity.Id,
            Source = OperationalEventSources.Api,
            OccurredAt = entity.ScrappedAt,
            Notes = entity.Reason
        });

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }
}
