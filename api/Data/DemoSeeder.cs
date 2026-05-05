using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;
using Resource = DriveTraceCore.Api.Models.Resource;

namespace DriveTraceCore.Api.Data;

public static class DemoSeeder
{
    public static async Task SeedAsync(DriveTraceDbContext db)
    {
        if (await db.Products.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var door = new Product { Name = "Automotive Door", Info = "Traceable automotive door subproduct used as the main V1 demo scenario." };
        var body = new Product { Name = "Car Body Module", Info = "Higher-level product that can consume door subproducts in final assembly." };
        var steering = new Product { Name = "Steering Wheel", Info = "Optional component for future line extension." };
        var seat = new Product { Name = "Seat", Info = "Optional interior subassembly." };
        db.Products.AddRange(door, body, steering, seat);
        await db.SaveChangesAsync();

        var standardDoor = new Variant { ProductId = door.Id, VariantCode = "DOOR-STD", Name = "Standard Door" };
        var premiumDoor = new Variant { ProductId = door.Id, VariantCode = "DOOR-PRM", Name = "Premium Door" };
        var reinforcedDoor = new Variant { ProductId = door.Id, VariantCode = "DOOR-REINF", Name = "Reinforced Door" };
        var sportInterior = new Variant { ProductId = seat.Id, VariantCode = "INT-SPORT", Name = "Sport Interior Variant" };
        db.Variants.AddRange(standardDoor, premiumDoor, reinforcedDoor, sportInterior);

        var line = new ProductionLine { LineCode = "DL-01", Name = "Door Assembly Line" };
        db.ProductionLines.Add(line);
        await db.SaveChangesAsync();

        var raw = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-RAW", Name = "Raw Materials", SectionType = "Warehouse" };
        var assign = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-SUPPORT", Name = "Support Assignment", SectionType = "Tracking" };
        var stamping = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-STAMP", Name = "Blanking / Stamping / Cutting", SectionType = "Production" };
        var welding = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-WELD", Name = "Hemming & Welding", SectionType = "Production" };
        var painting = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-PAINT", Name = "Painting", SectionType = "Production" };
        var quality = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-QC", Name = "Quality Control", SectionType = "Quality" };
        var rackStorage = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-RACK", Name = "Rack Storage", SectionType = "Post-line Logistics" };
        db.ProductionLineSections.AddRange(raw, assign, stamping, welding, painting, quality, rackStorage);
        await db.SaveChangesAsync();

        var resources = new[]
        {
            new Resource { Name = "Operator A", Type = "Operator", Function = "Support assignment and local validation" },
            new Resource { Name = "Robot Cell R1", Type = "Robot", Function = "Stamping and cutting assistance" },
            new Resource { Name = "Welding Robot WR-02", Type = "Robot", Function = "Hemming and welding" },
            new Resource { Name = "Paint Booth PB-01", Type = "Machine", Function = "Primer and final paint application" },
            new Resource { Name = "Quality Inspector QI-01", Type = "Operator", Function = "Visual and checkpoint quality control" }
        };
        db.Resources.AddRange(resources);

        var process = new ManufacturingProcess { ProductId = door.Id, ProcessName = "Door Manufacturing Process", Info = "Nominal linear process for WIP traceability of automotive door subproducts." };
        db.ManufacturingProcesses.Add(process);
        await db.SaveChangesAsync();

        var phases = new[]
        {
            new ManufacturingSectionPhase { SectionId = raw.Id, PhaseInfo = "Material Preparation", PhaseDuration = 20 },
            new ManufacturingSectionPhase { SectionId = assign.Id, PhaseInfo = "Support Assignment", PhaseDuration = 10 },
            new ManufacturingSectionPhase { SectionId = stamping.Id, PhaseInfo = "Stamping and Cutting", PhaseDuration = 35 },
            new ManufacturingSectionPhase { SectionId = welding.Id, PhaseInfo = "Hemming and Welding", PhaseDuration = 45 },
            new ManufacturingSectionPhase { SectionId = painting.Id, PhaseInfo = "Painting", PhaseDuration = 55 },
            new ManufacturingSectionPhase { SectionId = quality.Id, PhaseInfo = "Quality Inspection", PhaseDuration = 25 },
            new ManufacturingSectionPhase { SectionId = rackStorage.Id, PhaseInfo = "Post-line Storage", PhaseDuration = 15 }
        };
        db.ManufacturingSectionPhases.AddRange(phases);
        await db.SaveChangesAsync();

        db.ManufacturingProcessPhases.AddRange(phases.Select((phase, index) => new ManufacturingProcessPhase
        {
            ManufacturingProcessId = process.Id,
            ManufacturingPhaseId = phase.Id,
            NumberStepOrder = index + 1,
            ResourceId = index switch
            {
                1 => resources[0].Id,
                2 => resources[1].Id,
                3 => resources[2].Id,
                4 => resources[3].Id,
                5 => resources[4].Id,
                _ => null
            }
        }));

        var checkpoints = new[]
        {
            new Checkpoint { CheckpointCode = "CP-STAMP-01", Name = "Stamping Geometry Check", Status = "Active", SectionId = stamping.Id },
            new Checkpoint { CheckpointCode = "CP-WELD-01", Name = "Weld Seam Check", Status = "Active", SectionId = welding.Id },
            new Checkpoint { CheckpointCode = "CP-PAINT-01", Name = "Paint Thickness Check", Status = "Active", SectionId = painting.Id },
            new Checkpoint { CheckpointCode = "CP-QC-01", Name = "Final Door Quality Gate", Status = "Active", SectionId = quality.Id }
        };
        db.Checkpoints.AddRange(checkpoints);

        var order = new ManufacturingOrder
        {
            OrderNumber = "MO-DRIVE-DOOR-001",
            ProductId = door.Id,
            VariantId = standardDoor.Id,
            ManufacturingProcessId = process.Id,
            ProductionLineId = line.Id,
            PlannedQty = 5,
            ScheduledUntil = now.Date.AddDays(3).AddHours(17),
            Status = "In Progress",
            Observations = "Demo order for Door Assembly Line WIP traceability. Production is unitary; no final product lots are generated."
        };
        db.ManufacturingOrders.Add(order);
        await db.SaveChangesAsync();

        var units = new[]
        {
            new ProductUnit { ManufacturingOrderId = order.Id, VariantId = standardDoor.Id, UnitCode = "DU-001", UnitType = "Subproduct", Status = "Active", QualityStatus = "PASS", CurrentSectionId = quality.Id, CreatedAt = now.AddHours(-5) },
            new ProductUnit { ManufacturingOrderId = order.Id, VariantId = standardDoor.Id, UnitCode = "DU-002", UnitType = "Subproduct", Status = "Active", QualityStatus = "PASS", CurrentSectionId = painting.Id, CreatedAt = now.AddHours(-4) },
            new ProductUnit { ManufacturingOrderId = order.Id, VariantId = premiumDoor.Id, UnitCode = "DU-003", UnitType = "Subproduct", Status = "Blocked", QualityStatus = "FAIL", CurrentSectionId = quality.Id, CreatedAt = now.AddHours(-3) },
            new ProductUnit { ManufacturingOrderId = order.Id, VariantId = reinforcedDoor.Id, UnitCode = "DU-004", UnitType = "Subproduct", Status = "Rework", QualityStatus = "FAIL", CurrentSectionId = welding.Id, CreatedAt = now.AddHours(-2) },
            new ProductUnit { ManufacturingOrderId = order.Id, VariantId = standardDoor.Id, UnitCode = "DU-005", UnitType = "Subproduct", Status = "Active", QualityStatus = "Pending", CurrentSectionId = stamping.Id, CreatedAt = now.AddHours(-1) }
        };
        db.ProductUnits.AddRange(units);

        var supports = new[]
        {
            new Support { SupportCode = "SUP-001", Status = "Loaded", CurrentSectionId = quality.Id },
            new Support { SupportCode = "SUP-002", Status = "Loaded", CurrentSectionId = painting.Id },
            new Support { SupportCode = "SUP-003", Status = "Blocked", CurrentSectionId = quality.Id },
            new Support { SupportCode = "SUP-004", Status = "Rework", CurrentSectionId = welding.Id },
            new Support { SupportCode = "SUP-005", Status = "Loaded", CurrentSectionId = stamping.Id }
        };
        db.Supports.AddRange(supports);
        await db.SaveChangesAsync();

        for (var i = 0; i < units.Length; i++)
        {
            units[i].CurrentSupportId = supports[i].Id;
            db.UnitSupportAssignments.Add(new UnitSupportAssignment
            {
                ProductUnitId = units[i].Id,
                SupportId = supports[i].Id,
                DateTimeIn = units[i].CreatedAt.AddMinutes(5)
            });
        }
        await db.SaveChangesAsync();

        var orderedSections = new[] { raw, assign, stamping, welding, painting, quality };
        for (var i = 0; i < supports.Length; i++)
        {
            var maxStep = units[i].CurrentSectionId == quality.Id ? 6 : units[i].CurrentSectionId == painting.Id ? 5 : units[i].CurrentSectionId == welding.Id ? 4 : 3;
            for (var s = 0; s < maxStep; s++)
            {
                db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
                {
                    SupportId = supports[i].Id,
                    SectionId = orderedSections[s].Id,
                    EventType = s == 0 ? "SupportAssigned" : "Movement",
                    DateTime = units[i].CreatedAt.AddMinutes(15 + s * 28 + i * 3)
                });
            }
        }

        var rack1 = new Rack { RackCode = "RACK-001", Status = "Available", SectionId = rackStorage.Id };
        var rack2 = new Rack { RackCode = "RACK-002", Status = "Available", SectionId = rackStorage.Id };
        db.Racks.AddRange(rack1, rack2);

        var steel = new RawMaterial { Name = "Steel Sheet", Info = "Exterior panel steel used in the door structure." };
        var aluminium = new RawMaterial { Name = "Aluminium Panel", Info = "Lightweight panel alternative for premium variants." };
        var primer = new RawMaterial { Name = "Paint Primer", Info = "Primer paint used before final coating." };
        var finalPaint = new RawMaterial { Name = "Final Paint", Info = "Final coating for automotive door surface." };
        var rubber = new RawMaterial { Name = "Rubber Seal", Info = "Door sealing material." };
        var wiring = new RawMaterial { Name = "Wiring Clip", Info = "Small component used to retain wiring inside the door." };
        db.RawMaterials.AddRange(steel, aluminium, primer, finalPaint, rubber, wiring);
        await db.SaveChangesAsync();

        var lots = new[]
        {
            new LotRawMaterial { RawMaterialId = steel.Id, SectionId = stamping.Id, LotNumber = "ST-LOT-001", LotQuantity = 250, LotUnit = "kg" },
            new LotRawMaterial { RawMaterialId = aluminium.Id, SectionId = stamping.Id, LotNumber = "AL-LOT-001", LotQuantity = 120, LotUnit = "kg" },
            new LotRawMaterial { RawMaterialId = primer.Id, SectionId = painting.Id, LotNumber = "PAINT-LOT-001", LotQuantity = 80, LotUnit = "L" },
            new LotRawMaterial { RawMaterialId = finalPaint.Id, SectionId = painting.Id, LotNumber = "PAINT-LOT-002", LotQuantity = 120, LotUnit = "L" },
            new LotRawMaterial { RawMaterialId = rubber.Id, SectionId = welding.Id, LotNumber = "RUBBER-LOT-001", LotQuantity = 300, LotUnit = "m" },
            new LotRawMaterial { RawMaterialId = wiring.Id, SectionId = welding.Id, LotNumber = "WIRING-LOT-001", LotQuantity = 500, LotUnit = "pcs" }
        };
        db.LotRawMaterials.AddRange(lots);
        await db.SaveChangesAsync();

        foreach (var unit in units)
        {
            db.UnitMaterialLotUsages.Add(new UnitMaterialLotUsage { ProductUnitId = unit.Id, LotId = lots[0].Id, AssociationType = "Consumed", Quantity = 8 });
            db.UnitMaterialLotUsages.Add(new UnitMaterialLotUsage { ProductUnitId = unit.Id, LotId = lots[2].Id, AssociationType = "Consumed", Quantity = 1 });
            db.UnitMaterialLotUsages.Add(new UnitMaterialLotUsage { ProductUnitId = unit.Id, LotId = lots[4].Id, AssociationType = "Consumed", Quantity = 2 });
        }

        db.QualityResults.AddRange(
            new QualityResult { ProductUnitId = units[0].Id, CheckpointId = checkpoints[3].Id, Result = "PASS", RecordedAt = now.AddMinutes(-70), Notes = "Final inspection passed." },
            new QualityResult { ProductUnitId = units[1].Id, CheckpointId = checkpoints[2].Id, Result = "PASS", RecordedAt = now.AddMinutes(-55), Notes = "Paint thickness within tolerance." },
            new QualityResult { ProductUnitId = units[2].Id, CheckpointId = checkpoints[3].Id, Result = "FAIL", RecordedAt = now.AddMinutes(-45), Notes = "Door alignment deviation detected at final gate." },
            new QualityResult { ProductUnitId = units[3].Id, CheckpointId = checkpoints[1].Id, Result = "FAIL", RecordedAt = now.AddMinutes(-35), Notes = "Weld seam requires rework." });
        await db.SaveChangesAsync();

        var failDoor = await db.QualityResults.FirstAsync(x => x.ProductUnitId == units[2].Id && x.Result == "FAIL");
        var failWeld = await db.QualityResults.FirstAsync(x => x.ProductUnitId == units[3].Id && x.Result == "FAIL");

        var nc1 = new Nonconformity { ProductUnitId = units[2].Id, QualityResultId = failDoor.Id, Severity = "Major", Status = "Blocked", Description = "Alignment outside nominal tolerance; quality manager decision required." };
        var nc2 = new Nonconformity { ProductUnitId = units[3].Id, QualityResultId = failWeld.Id, Severity = "Medium", Status = "Rework", Description = "Weld seam underfill; route to controlled rework." };
        db.Nonconformities.AddRange(nc1, nc2);
        await db.SaveChangesAsync();

        db.ReworkRecords.Add(new ReworkRecord { ProductUnitId = units[3].Id, NonconformityId = nc2.Id, StartedAt = now.AddMinutes(-20), Status = "Open", Notes = "Repair weld seam and repeat quality checkpoint." });
        db.ScrapRecords.Add(new ScrapRecord { ProductUnitId = units[2].Id, NonconformityId = nc1.Id, ScrappedAt = now.AddMinutes(-10), Reason = "Demo scrap decision path; record retained for audit and genealogy." });
        db.Predictions.Add(new Prediction { ManufacturingOrderId = order.Id, ModelVersion = "future-v1", ModelType = "Placeholder", LastDate = now, CreatedAt = now });

        await db.SaveChangesAsync();
    }
}
