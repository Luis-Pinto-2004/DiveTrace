using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resource = DriveTraceCore.Api.Models.Resource;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
public abstract class CrudController<TEntity> : ControllerBase where TEntity : class, IEntity
{
    protected readonly DriveTraceDbContext Db;

    protected CrudController(DriveTraceDbContext db)
    {
        Db = db;
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TEntity>>> GetAll()
    {
        return Ok(await Db.Set<TEntity>().AsNoTracking().OrderBy(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public virtual async Task<ActionResult<TEntity>> GetById(int id)
    {
        var entity = await Db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TEntity>> Create(TEntity entity)
    {
        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<IActionResult> Update(int id, TEntity entity)
    {
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
        var entity = await Db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        Db.Set<TEntity>().Remove(entity);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

[Route("api/products")]
public sealed class ProductsController : CrudController<Product>
{
    public ProductsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/variants")]
public sealed class VariantsController : CrudController<Variant>
{
    public VariantsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/customers")]
public sealed class CustomersController : CrudController<Customer>
{
    public CustomersController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-orders")]
public sealed class ManufacturingOrdersController : CrudController<ManufacturingOrder>
{
    public ManufacturingOrdersController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/production-lines")]
public sealed class ProductionLinesController : CrudController<ProductionLine>
{
    public ProductionLinesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/production-line-sections")]
public sealed class ProductionLineSectionsController : CrudController<ProductionLineSection>
{
    public ProductionLineSectionsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/resources")]
public sealed class ResourcesController : CrudController<Resource>
{
    public ResourcesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-processes")]
public sealed class ManufacturingProcessesController : CrudController<ManufacturingProcess>
{
    public ManufacturingProcessesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-section-phases")]
public sealed class ManufacturingSectionPhasesController : CrudController<ManufacturingSectionPhase>
{
    public ManufacturingSectionPhasesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/manufacturing-process-phases")]
public sealed class ManufacturingProcessPhasesController : CrudController<ManufacturingProcessPhase>
{
    public ManufacturingProcessPhasesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/checkpoints")]
public sealed class CheckpointsController : CrudController<Checkpoint>
{
    public CheckpointsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/product-units")]
public sealed class ProductUnitsController : CrudController<ProductUnit>
{
    public ProductUnitsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/product-unit-location-history")]
public sealed class ProductUnitLocationHistoryController : CrudController<ProductUnitLocationHistory>
{
    public ProductUnitLocationHistoryController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/supports")]
public sealed class SupportsController : CrudController<Support>
{
    public SupportsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/unit-support-assignments")]
public sealed class UnitSupportAssignmentsController : CrudController<UnitSupportAssignment>
{
    public UnitSupportAssignmentsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/support-localization-history")]
public sealed class SupportLocalizationHistoryController : CrudController<SupportLocalizationHistory>
{
    public SupportLocalizationHistoryController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/racks")]
public sealed class RacksController : CrudController<Rack>
{
    public RacksController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/rack-support-assignments")]
public sealed class RackSupportAssignmentsController : CrudController<RackSupportAssignment>
{
    public RackSupportAssignmentsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/raw-materials")]
public sealed class RawMaterialsController : CrudController<RawMaterial>
{
    public RawMaterialsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/lot-raw-materials")]
public sealed class LotRawMaterialsController : CrudController<LotRawMaterial>
{
    public LotRawMaterialsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/unit-material-lot-usages")]
public sealed class UnitMaterialLotUsagesController : CrudController<UnitMaterialLotUsage>
{
    public UnitMaterialLotUsagesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/quality-results")]
public sealed class QualityResultsController : CrudController<QualityResult>
{
    public QualityResultsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/nonconformities")]
public sealed class NonconformitiesController : CrudController<Nonconformity>
{
    public NonconformitiesController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/rework-records")]
public sealed class ReworkRecordsController : CrudController<ReworkRecord>
{
    public ReworkRecordsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/scrap-records")]
public sealed class ScrapRecordsController : CrudController<ScrapRecord>
{
    public ScrapRecordsController(DriveTraceDbContext db) : base(db) { }
}

[Route("api/predictions")]
public sealed class PredictionsController : CrudController<Prediction>
{
    public PredictionsController(DriveTraceDbContext db) : base(db) { }
}
