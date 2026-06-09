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

        var door = new Product { Name = "Porta automóvel", Info = "Subproduto automóvel rastreável usado como cenário demo principal da V1." };
        var body = new Product { Name = "Módulo de carroçaria", Info = "Produto de nível superior que pode consumir subprodutos de porta na montagem final." };
        var steering = new Product { Name = "Volante", Info = "Componente opcional para futura extensão da linha." };
        var seat = new Product { Name = "Banco", Info = "Subconjunto interior opcional." };
        db.Products.AddRange(door, body, steering, seat);
        await db.SaveChangesAsync();

        var standardDoor = new Variant { ProductId = door.Id, VariantCode = "DOOR-STD", Name = "Porta standard" };
        var premiumDoor = new Variant { ProductId = door.Id, VariantCode = "DOOR-PRM", Name = "Porta premium" };
        var reinforcedDoor = new Variant { ProductId = door.Id, VariantCode = "DOOR-REINF", Name = "Porta reforçada" };
        var sportInterior = new Variant { ProductId = seat.Id, VariantCode = "INT-SPORT", Name = "Variante interior desportiva" };
        db.Variants.AddRange(standardDoor, premiumDoor, reinforcedDoor, sportInterior);

        var line = new ProductionLine { LineCode = "DL-01", Name = "Linha de Montagem de Portas" };
        db.ProductionLines.Add(line);
        await db.SaveChangesAsync();

        var raw = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-RAW", Name = "Matérias-primas", SectionType = "Armazém" };
        var assign = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-SUPPORT", Name = "Atribuição do suporte", SectionType = "Rastreio" };
        var stamping = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-STAMP", Name = "Corte / Estampagem", SectionType = "Produção" };
        var welding = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-WELD", Name = "Dobra e Soldadura", SectionType = "Produção" };
        var painting = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-PAINT", Name = "Pintura", SectionType = "Produção" };
        var quality = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-QC", Name = "Controlo de Qualidade", SectionType = "Qualidade" };
        var rackStorage = new ProductionLineSection { LineId = line.Id, SectionCode = "SEC-RACK", Name = "Armazenamento em Rack", SectionType = "Logística Pós-Linha" };
        db.ProductionLineSections.AddRange(raw, assign, stamping, welding, painting, quality, rackStorage);
        await db.SaveChangesAsync();

        var resources = new[]
        {
            new Resource { Name = "Operador A", Type = "Operador", Function = "Atribuição do suporte e validação local" },
            new Resource { Name = "Célula Robotizada R1", Type = "Robot", Function = "Apoio ao corte e estampagem" },
            new Resource { Name = "Robot de Soldadura WR-02", Type = "Robot", Function = "Dobra e soldadura" },
            new Resource { Name = "Cabina de Pintura PB-01", Type = "Máquina", Function = "Aplicação de primário e tinta final" },
            new Resource { Name = "Inspetor de Qualidade QI-01", Type = "Operador", Function = "Controlo visual e checkpoints de qualidade" }
        };
        db.Resources.AddRange(resources);

        var process = new ManufacturingProcess { ProductId = door.Id, ProcessName = "Processo de Fabrico de Porta", Info = "Processo linear nominal para rastreabilidade WIP de subprodutos de porta automóvel." };
        db.ManufacturingProcesses.Add(process);
        await db.SaveChangesAsync();

        var phases = new[]
        {
            new ManufacturingSectionPhase { SectionId = raw.Id, PhaseInfo = "Preparação de materiais", PhaseDuration = 20 },
            new ManufacturingSectionPhase { SectionId = assign.Id, PhaseInfo = "Atribuição do suporte", PhaseDuration = 10 },
            new ManufacturingSectionPhase { SectionId = stamping.Id, PhaseInfo = "Corte e Estampagem", PhaseDuration = 35 },
            new ManufacturingSectionPhase { SectionId = welding.Id, PhaseInfo = "Dobra e Soldadura", PhaseDuration = 45 },
            new ManufacturingSectionPhase { SectionId = painting.Id, PhaseInfo = "Pintura", PhaseDuration = 55 },
            new ManufacturingSectionPhase { SectionId = quality.Id, PhaseInfo = "Inspeção de Qualidade", PhaseDuration = 25 },
            new ManufacturingSectionPhase { SectionId = rackStorage.Id, PhaseInfo = "Armazenamento Pós-Linha", PhaseDuration = 15 }
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
            new Checkpoint { CheckpointCode = "CP-STAMP-01", Name = "Verificação de geometria na estampagem", Status = "Active", SectionId = stamping.Id },
            new Checkpoint { CheckpointCode = "CP-WELD-01", Name = "Verificação do cordão de soldadura", Status = "Active", SectionId = welding.Id },
            new Checkpoint { CheckpointCode = "CP-PAINT-01", Name = "Verificação da espessura de pintura", Status = "Active", SectionId = painting.Id },
            new Checkpoint { CheckpointCode = "CP-QC-01", Name = "Controlo final de qualidade da porta", Status = "Active", SectionId = quality.Id }
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
            Observations = "Ordem demo para rastreabilidade WIP da linha de montagem de portas. A produção é unitária; não são gerados lotes de produto final."
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

        var steel = new RawMaterial { Name = "Chapa de aço", Info = "Chapa de aço exterior usada na estrutura da porta." };
        var aluminium = new RawMaterial { Name = "Painel de alumínio", Info = "Painel leve usado em variantes premium." };
        var primer = new RawMaterial { Name = "Primário de pintura", Info = "Primário aplicado antes da pintura final." };
        var finalPaint = new RawMaterial { Name = "Tinta final", Info = "Revestimento final da superfície da porta." };
        var rubber = new RawMaterial { Name = "Vedante de borracha", Info = "Material de vedação da porta." };
        var wiring = new RawMaterial { Name = "Clip de cablagem", Info = "Componente usado para fixar cablagem no interior da porta." };
        db.RawMaterials.AddRange(steel, aluminium, primer, finalPaint, rubber, wiring);
        await db.SaveChangesAsync();

        var lots = new[]
        {
            new LotRawMaterial { RawMaterialId = steel.Id, SectionId = stamping.Id, LotNumber = "ST-LOT-001", LotQuantity = 250, LotUnit = "kg" },
            new LotRawMaterial { RawMaterialId = aluminium.Id, SectionId = stamping.Id, LotNumber = "AL-LOT-001", LotQuantity = 120, LotUnit = "kg" },
            new LotRawMaterial { RawMaterialId = primer.Id, SectionId = painting.Id, LotNumber = "PAINT-LOT-001", LotQuantity = 80, LotUnit = "L" },
            new LotRawMaterial { RawMaterialId = finalPaint.Id, SectionId = painting.Id, LotNumber = "PAINT-LOT-002", LotQuantity = 120, LotUnit = "L" },
            new LotRawMaterial { RawMaterialId = rubber.Id, SectionId = welding.Id, LotNumber = "RUBBER-LOT-001", LotQuantity = 300, LotUnit = "m" },
            new LotRawMaterial { RawMaterialId = wiring.Id, SectionId = welding.Id, LotNumber = "WIRING-LOT-001", LotQuantity = 500, LotUnit = "un." }
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
            new QualityResult { ProductUnitId = units[0].Id, CheckpointId = checkpoints[3].Id, Result = "PASS", RecordedAt = now.AddMinutes(-70), Notes = "Inspeção final aprovada." },
            new QualityResult { ProductUnitId = units[1].Id, CheckpointId = checkpoints[2].Id, Result = "PASS", RecordedAt = now.AddMinutes(-55), Notes = "Espessura da pintura dentro da tolerância." },
            new QualityResult { ProductUnitId = units[2].Id, CheckpointId = checkpoints[3].Id, Result = "FAIL", RecordedAt = now.AddMinutes(-45), Notes = "Desvio de alinhamento detetado no controlo final." },
            new QualityResult { ProductUnitId = units[3].Id, CheckpointId = checkpoints[1].Id, Result = "FAIL", RecordedAt = now.AddMinutes(-35), Notes = "Cordão de soldadura requer retrabalho." });
        await db.SaveChangesAsync();

        var failDoor = await db.QualityResults.FirstAsync(x => x.ProductUnitId == units[2].Id && x.Result == "FAIL");
        var failWeld = await db.QualityResults.FirstAsync(x => x.ProductUnitId == units[3].Id && x.Result == "FAIL");

        var nc1 = new Nonconformity { ProductUnitId = units[2].Id, QualityResultId = failDoor.Id, Severity = "Major", Status = "Blocked", Description = "Alinhamento fora da tolerância nominal; decisão do responsável de qualidade necessária." };
        var nc2 = new Nonconformity { ProductUnitId = units[3].Id, QualityResultId = failWeld.Id, Severity = "Medium", Status = "Rework", Description = "Falta de enchimento no cordão de soldadura; encaminhar para retrabalho controlado." };
        db.Nonconformities.AddRange(nc1, nc2);
        await db.SaveChangesAsync();

        db.ReworkRecords.Add(new ReworkRecord { ProductUnitId = units[3].Id, NonconformityId = nc2.Id, StartedAt = now.AddMinutes(-20), Status = "Open", Notes = "Reparar cordão de soldadura e repetir checkpoint de qualidade." });
        db.ScrapRecords.Add(new ScrapRecord { ProductUnitId = units[2].Id, NonconformityId = nc1.Id, ScrappedAt = now.AddMinutes(-10), Reason = "Caminho de decisão de sucata para demonstração; registo mantido para auditoria e genealogia." });
        db.Predictions.Add(new Prediction { ManufacturingOrderId = order.Id, ModelVersion = "future-v1", ModelType = "Placeholder", LastDate = now, CreatedAt = now });

        await db.SaveChangesAsync();
    }
}
