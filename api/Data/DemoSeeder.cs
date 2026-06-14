using DriveTraceCore.Api.Models;
using DriveTraceCore.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Resource = DriveTraceCore.Api.Models.Resource;

namespace DriveTraceCore.Api.Data;

public static class DemoSeeder
{
    private const string SeedSource = "demo-seed-pt-pt-v2";
    private const string PublicTrackingCode = "TRC-PORTA-001";
    private static readonly JsonSerializerOptions OperationalEventJsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task SeedAsync(DriveTraceDbContext db)
    {
        var now = DateTime.UtcNow;

        await NormalizeLegacyDemoCodesAsync(db);
        await NormalizeLegacyDemoTextsAsync(db);
        await db.SaveChangesAsync();

        var products = await SeedProductsAsync(db);
        await db.SaveChangesAsync();

        var variants = await SeedVariantsAsync(db, products["porta"]);
        await db.SaveChangesAsync();

        var lines = await SeedProductionLinesAsync(db);
        await db.SaveChangesAsync();

        var sections = await SeedSectionsAsync(db, lines);
        await db.SaveChangesAsync();

        var resources = await SeedResourcesAsync(db);
        await db.SaveChangesAsync();

        var process = await EnsureProcessAsync(
            db,
            products["porta"],
            "Processo de fabrico de porta automóvel",
            "Processo nominal para rastreabilidade WIP de subconjuntos de porta automóvel com transferências entre montagem, pintura, qualidade e logística pós-linha.");
        await db.SaveChangesAsync();

        await SeedProcessRouteAsync(db, process, sections, resources);
        await db.SaveChangesAsync();

        var checkpoints = await SeedCheckpointsAsync(db, sections);
        var customers = await SeedCustomersAsync(db);
        await db.SaveChangesAsync();

        var orders = await SeedOrdersAsync(db, now, customers, products, variants, process, lines);
        await db.SaveChangesAsync();

        var supports = await SeedSupportsAsync(db, sections);
        var racks = await SeedRacksAsync(db, sections);
        await db.SaveChangesAsync();

        var units = await SeedProductUnitsAsync(db, now, orders, variants, sections, supports);
        await db.SaveChangesAsync();

        await SeedSupportAssignmentsAsync(db, now, units, supports);
        await SeedMovementHistoryAsync(db, now, units, supports, sections);
        await SeedRackAssignmentsAsync(db, now, racks, supports);

        var materials = await SeedRawMaterialsAsync(db);
        await db.SaveChangesAsync();

        var lots = await SeedLotsAsync(db, materials, sections);
        await db.SaveChangesAsync();

        await SeedMaterialUsageAsync(db, units, lots);

        var quality = await SeedQualityAsync(db, now, units, checkpoints);
        await db.SaveChangesAsync();

        var nonconformities = await SeedNonconformitiesAsync(db, now, units, quality);
        await db.SaveChangesAsync();

        await SeedReworkAndScrapAsync(db, now, units, nonconformities);
        await db.SaveChangesAsync();

        await SeedOperationalEventsAsync(db, now, orders, units, supports, racks, sections, quality, nonconformities);
        await SeedPredictionsAsync(db, now, orders);
        await NormalizeAdHocLegacyDemoRowsAsync(db, now, customers, products, variants, process, lines, sections);

        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Product>> SeedProductsAsync(DriveTraceDbContext db)
    {
        return new Dictionary<string, Product>
        {
            ["porta"] = await EnsureProductAsync(
                db,
                "Porta automóvel",
                "Subconjunto automóvel rastreável usado como cenário principal de demonstração.",
                "Automotive door",
                "Automotive Door"),
            ["subconjunto"] = await EnsureProductAsync(
                db,
                "Subconjunto de porta",
                "Estrutura funcional que agrega painel exterior, reforços, fecho e cablagem."),
            ["painel"] = await EnsureProductAsync(
                db,
                "Painel exterior de porta",
                "Painel estampado preparado para soldadura, pintura e controlo dimensional."),
            ["estrutura"] = await EnsureProductAsync(
                db,
                "Estrutura interior de porta",
                "Estrutura interior com reforços e pontos de fixação para montagem final.")
        };
    }

    private static async Task<Dictionary<string, Variant>> SeedVariantsAsync(DriveTraceDbContext db, Product product)
    {
        return new Dictionary<string, Variant>
        {
            ["std"] = await EnsureVariantAsync(db, product, "PORTA-STD", "Porta standard", "DOOR-STD"),
            ["prm"] = await EnsureVariantAsync(db, product, "PORTA-PRM", "Porta premium", "DOOR-PRM"),
            ["ref"] = await EnsureVariantAsync(db, product, "PORTA-REF", "Porta reforçada", "DOOR-REINF"),
            ["lev"] = await EnsureVariantAsync(db, product, "PORTA-LEV", "Porta leve")
        };
    }

    private static async Task<Dictionary<string, ProductionLine>> SeedProductionLinesAsync(DriveTraceDbContext db)
    {
        return new Dictionary<string, ProductionLine>
        {
            ["montagem"] = await EnsureProductionLineAsync(db, "LINHA-01", "Linha 1 - Montagem e soldadura", 10, "montagem", "DL-01"),
            ["pinturaA"] = await EnsureProductionLineAsync(db, "LINHA-02", "Linha 2 - Pintura A", 20, "pintura", "DL-02"),
            ["pinturaB"] = await EnsureProductionLineAsync(db, "LINHA-03", "Linha 3 - Pintura B", 30, "pintura", "DL-03"),
            ["qualidade"] = await EnsureProductionLineAsync(db, "LINHA-04", "Linha 4 - Qualidade e retrabalho", 40, "qualidade", "DL-04")
        };
    }

    private static async Task<Dictionary<string, ProductionLineSection>> SeedSectionsAsync(
        DriveTraceDbContext db,
        IReadOnlyDictionary<string, ProductionLine> lines)
    {
        return new Dictionary<string, ProductionLineSection>
        {
            ["mp"] = await EnsureSectionAsync(db, lines["montagem"], "SEC-MP", "Matérias-primas", "Armazém", 10, 1, 1, "entrada", false, false, false, "SEC-RAW"),
            ["atrib"] = await EnsureSectionAsync(db, lines["montagem"], "SEC-ATRIB-SUP", "Atribuição de suporte", "Rastreio", 20, 2, 1, "rastreio", true, true, true, "SEC-SUPPORT"),
            ["corte"] = await EnsureSectionAsync(db, lines["montagem"], "SEC-CORTE-ESTAMP", "Corte e estampagem", "Produção", 30, 3, 1, "fabrico", false, false, false, "SEC-STAMP"),
            ["sold"] = await EnsureSectionAsync(db, lines["montagem"], "SEC-SOLD", "Soldadura", "Produção", 40, 4, 1, "fabrico", false, false, true, "SEC-WELD"),
            ["transPint"] = await EnsureSectionAsync(db, lines["montagem"], "SEC-TRANS-PINT", "Buffer de transferência para pintura", "Transferência", 50, 5, 1, "transferencia", true, true, true, "SEC-PAINT"),
            ["cq"] = await EnsureSectionAsync(db, lines["qualidade"], "SEC-CQ", "Controlo de qualidade", "Qualidade", 20, 2, 4, "qualidade", true, true, true, "SEC-QC"),
            ["rack"] = await EnsureSectionAsync(db, lines["qualidade"], "SEC-RACK", "Armazenamento em rack", "Logística pós-linha", 40, 4, 4, "pos-linha", false, true, false),
            ["prepA"] = await EnsureSectionAsync(db, lines["pinturaA"], "SEC-PREP-PINT-A", "Preparação de pintura A", "Produção", 10, 1, 2, "pintura", true, true, false, "SEC-PAINT-PREP-A"),
            ["pintA"] = await EnsureSectionAsync(db, lines["pinturaA"], "SEC-PINT-A", "Pintura A", "Produção", 20, 2, 2, "pintura", false, false, true, "SEC-PAINT-A"),
            ["curaA"] = await EnsureSectionAsync(db, lines["pinturaA"], "SEC-CURA-A", "Cura A", "Produção", 30, 3, 2, "pintura", true, false, true, "SEC-CURE-A"),
            ["pintB"] = await EnsureSectionAsync(db, lines["pinturaB"], "SEC-PINT-B", "Pintura B", "Produção", 10, 1, 3, "pintura", true, true, true, "SEC-PAINT-B"),
            ["inspB"] = await EnsureSectionAsync(db, lines["pinturaB"], "SEC-INSPEC-PINT-B", "Inspeção de pintura B", "Qualidade", 20, 2, 3, "qualidade-pintura", true, false, true, "SEC-PAINT-INSPECT-B"),
            ["montFinal"] = await EnsureSectionAsync(db, lines["qualidade"], "SEC-MONT-FINAL", "Montagem final", "Produção", 10, 1, 4, "montagem-final", true, true, true, "SEC-FINAL-ASSY"),
            ["retrabalho"] = await EnsureSectionAsync(db, lines["qualidade"], "SEC-RETRAB", "Retrabalho", "Retrabalho", 30, 3, 4, "retrabalho", true, true, true, "SEC-REWORK-CELL"),
            ["exped"] = await EnsureSectionAsync(db, lines["qualidade"], "SEC-EXPED", "Buffer de expedição", "Logística pós-linha", 50, 5, 4, "pos-linha", false, true, false, "SEC-EXPEDITION")
        };
    }

    private static async Task<Dictionary<string, Resource>> SeedResourcesAsync(DriveTraceDbContext db)
    {
        return new Dictionary<string, Resource>
        {
            ["operador"] = await EnsureResourceAsync(db, "Operador de montagem OM-01", "Operador", "Atribuição de suporte e validação local", "Operador A"),
            ["robotCorte"] = await EnsureResourceAsync(db, "Célula robotizada CR-01", "Robot", "Apoio ao corte e estampagem", "Célula Robotizada R1"),
            ["robotSold"] = await EnsureResourceAsync(db, "Robot de soldadura RS-02", "Robot", "Dobra e soldadura do subconjunto de porta", "Robot de Soldadura WR-02"),
            ["cabina"] = await EnsureResourceAsync(db, "Cabina de pintura CP-01", "Máquina", "Aplicação de primário, tinta base e verniz", "Cabina de Pintura PB-01"),
            ["inspetor"] = await EnsureResourceAsync(db, "Inspetor de qualidade CQ-01", "Operador", "Controlo visual e pontos de controlo de qualidade", "Inspetor de Qualidade QI-01"),
            ["retrabalho"] = await EnsureResourceAsync(db, "Técnico de retrabalho TR-01", "Operador", "Correção controlada de não conformidades")
        };
    }

    private static async Task SeedProcessRouteAsync(
        DriveTraceDbContext db,
        ManufacturingProcess process,
        IReadOnlyDictionary<string, ProductionLineSection> sections,
        IReadOnlyDictionary<string, Resource> resources)
    {
        var route = new[]
        {
            (Key: "mp", Phase: "Preparação de matérias-primas", Duration: 20, Resource: (Resource?)null),
            (Key: "atrib", Phase: "Atribuição de suporte", Duration: 10, Resource: resources["operador"]),
            (Key: "corte", Phase: "Corte e estampagem", Duration: 35, Resource: resources["robotCorte"]),
            (Key: "sold", Phase: "Soldadura", Duration: 45, Resource: resources["robotSold"]),
            (Key: "transPint", Phase: "Transferência para pintura", Duration: 12, Resource: resources["operador"]),
            (Key: "pintA", Phase: "Pintura A", Duration: 55, Resource: resources["cabina"]),
            (Key: "pintB", Phase: "Pintura B", Duration: 55, Resource: resources["cabina"]),
            (Key: "cq", Phase: "Controlo final de qualidade", Duration: 25, Resource: resources["inspetor"]),
            (Key: "retrabalho", Phase: "Retrabalho controlado", Duration: 40, Resource: resources["retrabalho"]),
            (Key: "rack", Phase: "Armazenamento pós-linha", Duration: 15, Resource: (Resource?)null)
        };

        for (var index = 0; index < route.Length; index++)
        {
            var item = route[index];
            var phase = await EnsureSectionPhaseAsync(db, sections[item.Key], item.Phase, item.Duration);
            await db.SaveChangesAsync();
            await EnsureProcessPhaseAsync(db, process, phase, index + 1, item.Resource);
        }
    }

    private static async Task<Dictionary<string, Checkpoint>> SeedCheckpointsAsync(
        DriveTraceDbContext db,
        IReadOnlyDictionary<string, ProductionLineSection> sections)
    {
        return new Dictionary<string, Checkpoint>
        {
            ["estamp"] = await EnsureCheckpointAsync(db, "PC-ESTAMP-001", "Verificação geométrica da estampagem", "Active", sections["corte"], "CP-STAMP-01"),
            ["sold"] = await EnsureCheckpointAsync(db, "PC-SOLD-001", "Controlo do cordão de soldadura", "Active", sections["sold"], "CP-WELD-01"),
            ["pint"] = await EnsureCheckpointAsync(db, "PC-PINT-001", "Verificação da espessura da pintura", "Active", sections["pintA"], "CP-PAINT-01"),
            ["pintB"] = await EnsureCheckpointAsync(db, "PC-PINT-002", "Inspeção visual da pintura B", "Active", sections["inspB"]),
            ["cq"] = await EnsureCheckpointAsync(db, "PC-CQ-001", "Controlo final da porta", "Active", sections["cq"], "CP-QC-01"),
            ["retrabalho"] = await EnsureCheckpointAsync(db, "PC-RETRAB-001", "Validação pós-retrabalho", "Active", sections["retrabalho"])
        };
    }

    private static async Task<Dictionary<string, Customer>> SeedCustomersAsync(DriveTraceDbContext db)
    {
        return new Dictionary<string, Customer>
        {
            ["auto"] = await EnsureCustomerAsync(db, "CLI-AUTO-001", "AutoEuropa Demo", "planeamento@autoeuropa-demo.pt", "CUST-DEMO"),
            ["oem"] = await EnsureCustomerAsync(db, "CLI-OEM-002", "Fornecedor OEM Norte", "qualidade@oem-norte.pt"),
            ["piloto"] = await EnsureCustomerAsync(db, "CLI-PILOTO-003", "Linha Piloto DRIVOLUTION", "piloto@drivolution.pt")
        };
    }

    private static async Task<Dictionary<string, ManufacturingOrder>> SeedOrdersAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, Customer> customers,
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyDictionary<string, Variant> variants,
        ManufacturingProcess process,
        IReadOnlyDictionary<string, ProductionLine> lines)
    {
        return new Dictionary<string, ManufacturingOrder>
        {
            ["of1"] = await EnsureOrderAsync(db, "OF-PORTA-001", customers["auto"], products["porta"], variants["std"], process, lines["montagem"], 6, now.Date.AddDays(3).AddHours(17), "In Progress", "OEM-PT-2026-001", PublicTrackingCode, "Ordem demonstrativa para rastreabilidade WIP de portas automóveis.", "MO-DRIVE-DOOR-001", "DT-DEMO-001"),
            ["of2"] = await EnsureOrderAsync(db, "OF-PORTA-002", customers["oem"], products["porta"], variants["prm"], process, lines["montagem"], 4, now.Date.AddDays(4).AddHours(16), "In Progress", "OEM-PT-2026-002", "TRC-PORTA-002", "Ordem com transferência para pintura alternativa."),
            ["of3"] = await EnsureOrderAsync(db, "OF-PORTA-003", customers["piloto"], products["subconjunto"], variants["ref"], process, lines["montagem"], 3, now.Date.AddDays(5).AddHours(15), "Blocked", "PILOTO-2026-003", "TRC-PORTA-003", "Ordem usada para cenário de falha de qualidade e retrabalho."),
            ["of4"] = await EnsureOrderAsync(db, "OF-PORTA-004", customers["auto"], products["porta"], variants["lev"], process, lines["montagem"], 5, now.Date.AddDays(6).AddHours(14), "Planned", "OEM-PT-2026-004", "TRC-PORTA-004", "Ordem planeada para validação de capacidade pós-linha.")
        };
    }

    private static async Task<Dictionary<string, Support>> SeedSupportsAsync(
        DriveTraceDbContext db,
        IReadOnlyDictionary<string, ProductionLineSection> sections)
    {
        return new Dictionary<string, Support>
        {
            ["sup1"] = await EnsureSupportAsync(db, "SUP-001", "Loaded", sections["cq"]),
            ["sup2"] = await EnsureSupportAsync(db, "SUP-002", "Loaded", sections["cq"]),
            ["sup3"] = await EnsureSupportAsync(db, "SUP-003", "Rework", sections["retrabalho"]),
            ["sup4"] = await EnsureSupportAsync(db, "SUP-004", "Blocked", sections["sold"]),
            ["sup5"] = await EnsureSupportAsync(db, "SUP-005", "Loaded", sections["corte"]),
            ["sup6"] = await EnsureSupportAsync(db, "SUP-006", "Stored", sections["rack"]),
            ["sup7"] = await EnsureSupportAsync(db, "SUP-007", "Loaded", sections["pintB"]),
            ["sup8"] = await EnsureSupportAsync(db, "SUP-008", "Available", sections["atrib"])
        };
    }

    private static async Task<Dictionary<string, Rack>> SeedRacksAsync(
        DriveTraceDbContext db,
        IReadOnlyDictionary<string, ProductionLineSection> sections)
    {
        return new Dictionary<string, Rack>
        {
            ["rack1"] = await EnsureRackAsync(db, "RACK-001", "Stored", sections["rack"]),
            ["rack2"] = await EnsureRackAsync(db, "RACK-002", "Available", sections["rack"]),
            ["rack3"] = await EnsureRackAsync(db, "RACK-003", "Available", sections["rack"]),
            ["rack4"] = await EnsureRackAsync(db, "RACK-004", "Blocked", sections["exped"])
        };
    }

    private static async Task<Dictionary<string, ProductUnit>> SeedProductUnitsAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ManufacturingOrder> orders,
        IReadOnlyDictionary<string, Variant> variants,
        IReadOnlyDictionary<string, ProductionLineSection> sections,
        IReadOnlyDictionary<string, Support> supports)
    {
        return new Dictionary<string, ProductUnit>
        {
            ["u1"] = await EnsureProductUnitAsync(db, orders["of1"], variants["std"], "UP-PORTA-001", "Subproduto", "Completed", "PASS", sections["cq"], supports["sup1"], now.AddHours(-8), now.AddMinutes(-35), "DU-001"),
            ["u2"] = await EnsureProductUnitAsync(db, orders["of1"], variants["prm"], "UP-PORTA-002", "Subproduto", "Completed", "PASS", sections["cq"], supports["sup2"], now.AddHours(-7), now.AddMinutes(-30), "DU-002"),
            ["u3"] = await EnsureProductUnitAsync(db, orders["of3"], variants["prm"], "UP-PORTA-003", "Subproduto", "Rework", "FAIL", sections["retrabalho"], supports["sup3"], now.AddHours(-6), null, "DU-003"),
            ["u4"] = await EnsureProductUnitAsync(db, orders["of3"], variants["ref"], "UP-PORTA-004", "Subproduto", "Blocked", "FAIL", sections["sold"], supports["sup4"], now.AddHours(-5), null, "DU-004"),
            ["u5"] = await EnsureProductUnitAsync(db, orders["of1"], variants["std"], "UP-PORTA-005", "Subproduto", "Active", "Pending", sections["corte"], supports["sup5"], now.AddHours(-4), null, "DU-005"),
            ["u6"] = await EnsureProductUnitAsync(db, orders["of2"], variants["lev"], "UP-PORTA-006", "Subproduto", "Completed", "PASS", sections["rack"], supports["sup6"], now.AddHours(-3), now.AddMinutes(-20), "DU-006"),
            ["u7"] = await EnsureProductUnitAsync(db, orders["of2"], variants["prm"], "UP-PORTA-007", "Subproduto", "Active", "Pending", sections["pintB"], supports["sup7"], now.AddHours(-2), null),
            ["u8"] = await EnsureProductUnitAsync(db, orders["of4"], variants["lev"], "UP-PORTA-008", "Subproduto", "Active", "Pending", sections["atrib"], supports["sup8"], now.AddHours(-1), null),
            ["u9"] = await EnsureProductUnitAsync(db, orders["of4"], variants["std"], "UP-PORTA-009", "Subproduto", "Planned", "Pending", sections["mp"], null, now.AddMinutes(-45), null),
            ["u10"] = await EnsureProductUnitAsync(db, orders["of4"], variants["ref"], "UP-PORTA-010", "Subproduto", "Planned", "Pending", sections["mp"], null, now.AddMinutes(-30), null)
        };
    }

    private static async Task SeedSupportAssignmentsAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, Support> supports)
    {
        await EnsureOpenSupportAssignmentAsync(db, units["u1"], supports["sup1"], now.AddHours(-8).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u2"], supports["sup2"], now.AddHours(-7).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u3"], supports["sup3"], now.AddHours(-6).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u4"], supports["sup4"], now.AddHours(-5).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u5"], supports["sup5"], now.AddHours(-4).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u6"], supports["sup6"], now.AddHours(-3).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u7"], supports["sup7"], now.AddHours(-2).AddMinutes(5));
        await EnsureOpenSupportAssignmentAsync(db, units["u8"], supports["sup8"], now.AddHours(-1).AddMinutes(5));
    }

    private static async Task SeedMovementHistoryAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, Support> supports,
        IReadOnlyDictionary<string, ProductionLineSection> sections)
    {
        await EnsureSupportLocalizationAsync(db, supports["sup1"], sections["corte"], "SupportAssigned", now.AddHours(-7).AddMinutes(-50));
        await EnsureSupportLocalizationAsync(db, supports["sup1"], sections["sold"], "Movement", now.AddHours(-7).AddMinutes(-10));
        await EnsureSupportLocalizationAsync(db, supports["sup1"], sections["pintA"], "LineTransfer", now.AddHours(-6).AddMinutes(-20));
        await EnsureSupportLocalizationAsync(db, supports["sup1"], sections["cq"], "LineTransfer", now.AddHours(-5).AddMinutes(-30));

        await EnsureSupportLocalizationAsync(db, supports["sup2"], sections["sold"], "SupportAssigned", now.AddHours(-6).AddMinutes(-30));
        await EnsureSupportLocalizationAsync(db, supports["sup2"], sections["pintB"], "LineTransfer", now.AddHours(-5).AddMinutes(-40));
        await EnsureSupportLocalizationAsync(db, supports["sup2"], sections["cq"], "LineTransfer", now.AddHours(-4).AddMinutes(-45));

        await EnsureSupportLocalizationAsync(db, supports["sup3"], sections["sold"], "SupportAssigned", now.AddHours(-5).AddMinutes(-50));
        await EnsureSupportLocalizationAsync(db, supports["sup3"], sections["pintA"], "LineTransfer", now.AddHours(-4).AddMinutes(-50));
        await EnsureSupportLocalizationAsync(db, supports["sup3"], sections["cq"], "LineTransfer", now.AddHours(-3).AddMinutes(-50));
        await EnsureSupportLocalizationAsync(db, supports["sup3"], sections["retrabalho"], "LineTransfer", now.AddHours(-2).AddMinutes(-50));

        await EnsureSupportLocalizationAsync(db, supports["sup4"], sections["corte"], "SupportAssigned", now.AddHours(-4).AddMinutes(-50));
        await EnsureSupportLocalizationAsync(db, supports["sup4"], sections["sold"], "Movement", now.AddHours(-4).AddMinutes(-5));

        await EnsureSupportLocalizationAsync(db, supports["sup5"], sections["atrib"], "SupportAssigned", now.AddHours(-3).AddMinutes(-50));
        await EnsureSupportLocalizationAsync(db, supports["sup5"], sections["corte"], "Movement", now.AddHours(-3).AddMinutes(-20));

        await EnsureSupportLocalizationAsync(db, supports["sup6"], sections["cq"], "LineTransfer", now.AddHours(-2).AddMinutes(-15));
        await EnsureSupportLocalizationAsync(db, supports["sup6"], sections["rack"], "TransferToRack", now.AddHours(-1).AddMinutes(-30));

        await EnsureProductTransferHistoryAsync(db, units["u1"], null, sections["corte"], null, supports["sup1"], "SupportAssigned", "Entrada da unidade em corte e estampagem.", now.AddHours(-7).AddMinutes(-50));
        await EnsureProductTransferHistoryAsync(db, units["u1"], sections["corte"], sections["sold"], supports["sup1"], supports["sup1"], "Movement", "Transferência da estampagem para soldadura.", now.AddHours(-7).AddMinutes(-10));
        await EnsureProductTransferHistoryAsync(db, units["u1"], sections["sold"], sections["pintA"], supports["sup1"], supports["sup1"], "LineTransfer", "Transferência da soldadura para pintura A.", now.AddHours(-6).AddMinutes(-20));
        await EnsureProductTransferHistoryAsync(db, units["u1"], sections["pintA"], sections["cq"], supports["sup1"], supports["sup1"], "LineTransfer", "Transferência da pintura A para controlo de qualidade.", now.AddHours(-5).AddMinutes(-30));

        await EnsureProductTransferHistoryAsync(db, units["u2"], sections["sold"], sections["pintB"], supports["sup2"], supports["sup2"], "LineTransfer", "Transferência para pintura alternativa B.", now.AddHours(-5).AddMinutes(-40));
        await EnsureProductTransferHistoryAsync(db, units["u2"], sections["pintB"], sections["cq"], supports["sup2"], supports["sup2"], "LineTransfer", "Transferência da pintura B para controlo de qualidade.", now.AddHours(-4).AddMinutes(-45));

        await EnsureProductTransferHistoryAsync(db, units["u3"], sections["sold"], sections["pintA"], supports["sup3"], supports["sup3"], "LineTransfer", "Transferência da soldadura para pintura A.", now.AddHours(-4).AddMinutes(-50));
        await EnsureProductTransferHistoryAsync(db, units["u3"], sections["pintA"], sections["cq"], supports["sup3"], supports["sup3"], "LineTransfer", "Transferência para controlo de qualidade após pintura.", now.AddHours(-3).AddMinutes(-50));
        await EnsureProductTransferHistoryAsync(db, units["u3"], sections["cq"], sections["retrabalho"], supports["sup3"], supports["sup3"], "LineTransfer", "Encaminhamento para retrabalho por espessura de pintura fora de tolerância.", now.AddHours(-2).AddMinutes(-50));

        await EnsureProductTransferHistoryAsync(db, units["u4"], sections["corte"], sections["sold"], supports["sup4"], supports["sup4"], "Movement", "Movimento para soldadura com desalinhamento estrutural detetado.", now.AddHours(-4).AddMinutes(-5));
        await EnsureProductTransferHistoryAsync(db, units["u5"], sections["atrib"], sections["corte"], supports["sup5"], supports["sup5"], "Movement", "Unidade em produção, ainda sem controlo final.", now.AddHours(-3).AddMinutes(-20));
        await EnsureProductTransferHistoryAsync(db, units["u6"], sections["cq"], sections["rack"], supports["sup6"], supports["sup6"], "TransferToRack", "Unidade aprovada e associada a rack pós-linha.", now.AddHours(-1).AddMinutes(-30));
        await EnsureProductLocationSnapshotAsync(db, units["u7"], sections["pintB"], supports["sup7"], now.AddHours(-1).AddMinutes(-15));
        await EnsureProductLocationSnapshotAsync(db, units["u8"], sections["atrib"], supports["sup8"], now.AddMinutes(-55));
    }

    private static async Task SeedRackAssignmentsAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, Rack> racks,
        IReadOnlyDictionary<string, Support> supports)
    {
        await EnsureRackAssignmentAsync(db, racks["rack1"], supports["sup6"], now.AddHours(-1).AddMinutes(-25));
    }

    private static async Task<Dictionary<string, RawMaterial>> SeedRawMaterialsAsync(DriveTraceDbContext db)
    {
        return new Dictionary<string, RawMaterial>
        {
            ["aco"] = await EnsureRawMaterialAsync(db, "Chapa de aço DX56", "Chapa exterior usada na estrutura da porta.", "Chapa de aço", "Steel Sheet"),
            ["reforco"] = await EnsureRawMaterialAsync(db, "Reforço interior da porta", "Reforço estrutural aplicado antes da soldadura."),
            ["primario"] = await EnsureRawMaterialAsync(db, "Primário anticorrosivo", "Primário aplicado antes da tinta base.", "Primário de pintura", "Paint Primer"),
            ["tinta"] = await EnsureRawMaterialAsync(db, "Tinta base", "Tinta final aplicada na superfície exterior.", "Tinta final", "Final Paint"),
            ["verniz"] = await EnsureRawMaterialAsync(db, "Verniz", "Verniz de acabamento para proteção da pintura."),
            ["selante"] = await EnsureRawMaterialAsync(db, "Selante estrutural", "Selante usado nas uniões e zonas de vedação.", "Vedante de borracha", "Rubber Seal"),
            ["dobradica"] = await EnsureRawMaterialAsync(db, "Dobradiça", "Componente mecânico para montagem final da porta."),
            ["fecho"] = await EnsureRawMaterialAsync(db, "Fecho da porta", "Mecanismo de fecho para montagem final."),
            ["cablagem"] = await EnsureRawMaterialAsync(db, "Cablagem da porta", "Conjunto de cablagem e clips de fixação.", "Clip de cablagem", "Wiring Clip", "Aluminium Panel")
        };
    }

    private static async Task<Dictionary<string, LotRawMaterial>> SeedLotsAsync(
        DriveTraceDbContext db,
        IReadOnlyDictionary<string, RawMaterial> materials,
        IReadOnlyDictionary<string, ProductionLineSection> sections)
    {
        return new Dictionary<string, LotRawMaterial>
        {
            ["aco"] = await EnsureLotAsync(db, materials["aco"], sections["corte"], "LOTE-ACO-2026-001", 250, "kg", "ST-LOT-001"),
            ["reforco"] = await EnsureLotAsync(db, materials["reforco"], sections["sold"], "LOTE-REFORCO-2026-002", 160, "un."),
            ["primario"] = await EnsureLotAsync(db, materials["primario"], sections["pintA"], "LOTE-PRIMARIO-2026-011", 80, "L", "PAINT-LOT-001"),
            ["tinta"] = await EnsureLotAsync(db, materials["tinta"], sections["pintA"], "LOTE-TINTA-2026-014", 120, "L", "PAINT-LOT-002"),
            ["verniz"] = await EnsureLotAsync(db, materials["verniz"], sections["pintB"], "LOTE-VERNIZ-2026-006", 90, "L"),
            ["selante"] = await EnsureLotAsync(db, materials["selante"], sections["sold"], "LOTE-SELANTE-2026-003", 300, "m", "RUBBER-LOT-001"),
            ["dobradica"] = await EnsureLotAsync(db, materials["dobradica"], sections["montFinal"], "LOTE-DOBRADICA-2026-008", 240, "un."),
            ["fecho"] = await EnsureLotAsync(db, materials["fecho"], sections["montFinal"], "LOTE-FECHO-2026-009", 220, "un."),
            ["cablagem"] = await EnsureLotAsync(db, materials["cablagem"], sections["montFinal"], "LOTE-CABLAGEM-2026-010", 500, "un.", "WIRING-LOT-001")
        };
    }

    private static async Task SeedMaterialUsageAsync(
        DriveTraceDbContext db,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, LotRawMaterial> lots)
    {
        foreach (var unit in units.Values.Where(unit => unit.UnitCode is not "UP-PORTA-009" and not "UP-PORTA-010"))
        {
            await EnsureMaterialUsageAsync(db, unit, lots["aco"], "Consumido", 8);
            await EnsureMaterialUsageAsync(db, unit, lots["primario"], "Consumido", 1);
            await EnsureMaterialUsageAsync(db, unit, lots["selante"], "Consumido", 2);
        }

        await EnsureMaterialUsageAsync(db, units["u1"], lots["dobradica"], "Reservado", 2);
        await EnsureMaterialUsageAsync(db, units["u2"], lots["verniz"], "Consumido", 1);
        await EnsureMaterialUsageAsync(db, units["u6"], lots["cablagem"], "Consumido", 1);
    }

    private static async Task<Dictionary<string, QualityResult>> SeedQualityAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, Checkpoint> checkpoints)
    {
        return new Dictionary<string, QualityResult>
        {
            ["q1"] = await EnsureQualityResultAsync(db, units["u1"], checkpoints["cq"], "PASS", now.AddMinutes(-90), "Controlo final aprovado."),
            ["q2"] = await EnsureQualityResultAsync(db, units["u2"], checkpoints["pintB"], "PASS", now.AddMinutes(-80), "Pintura B aprovada sem desvios."),
            ["q3"] = await EnsureQualityResultAsync(db, units["u3"], checkpoints["pint"], "FAIL", now.AddMinutes(-70), "Espessura de pintura fora de tolerância."),
            ["q4"] = await EnsureQualityResultAsync(db, units["u4"], checkpoints["sold"], "FAIL", now.AddMinutes(-65), "Desalinhamento estrutural detetado na soldadura."),
            ["q5"] = await EnsureQualityResultAsync(db, units["u6"], checkpoints["cq"], "PASS", now.AddMinutes(-55), "Unidade aprovada para armazenamento pós-linha."),
            ["q6"] = await EnsureQualityResultAsync(db, units["u7"], checkpoints["pintB"], "PASS", now.AddMinutes(-35), "Inspeção visual da pintura sem defeitos críticos.")
        };
    }

    private static async Task<Dictionary<string, Nonconformity>> SeedNonconformitiesAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, QualityResult> quality)
    {
        return new Dictionary<string, Nonconformity>
        {
            ["nc1"] = await EnsureNonconformityAsync(db, units["u3"], quality["q3"], "Maior", "Rework", "Espessura de pintura fora de tolerância; encaminhar para retrabalho controlado.", now.AddMinutes(-68)),
            ["nc2"] = await EnsureNonconformityAsync(db, units["u4"], quality["q4"], "Crítica", "Blocked", "Desalinhamento estrutural na zona de soldadura; decisão de qualidade obrigatória.", now.AddMinutes(-63)),
            ["nc3"] = await EnsureNonconformityAsync(db, units["u7"], quality["q6"], "Menor", "Open", "Marcas superficiais ligeiras para acompanhamento na próxima inspeção.", now.AddMinutes(-30))
        };
    }

    private static async Task SeedReworkAndScrapAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, Nonconformity> nonconformities)
    {
        await EnsureReworkRecordAsync(db, units["u3"], nonconformities["nc1"], now.AddMinutes(-45), "Open", "Rever espessura de pintura, corrigir camada e repetir ponto de controlo.");
        await EnsureReworkRecordAsync(db, units["u4"], nonconformities["nc2"], now.AddMinutes(-40), "Open", "Validar desalinhamento estrutural antes de decidir recuperação ou sucata.");
        await EnsureScrapRecordAsync(db, units["u4"], nonconformities["nc2"], now.AddMinutes(-20), "Cenário demonstrativo de decisão de sucata por desalinhamento estrutural.");
    }

    private static async Task SeedOperationalEventsAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ManufacturingOrder> orders,
        IReadOnlyDictionary<string, ProductUnit> units,
        IReadOnlyDictionary<string, Support> supports,
        IReadOnlyDictionary<string, Rack> racks,
        IReadOnlyDictionary<string, ProductionLineSection> sections,
        IReadOnlyDictionary<string, QualityResult> quality,
        IReadOnlyDictionary<string, Nonconformity> nonconformities)
    {
        await EnsureOperationalEventAsync(db, new OperationalEvent
        {
            EventCode = "SEED-DEMO-PT-PT-V2",
            EventType = OperationalEventTypes.DemoSeeded,
            Source = OperationalEventSources.Seed,
            OccurredAt = now.AddHours(-8).AddMinutes(-10),
            Notes = "Dados demonstrativos PT-PT inicializados para a Fase A.",
            IsDemo = true,
            MetadataJson = SerializeMetadata(new Dictionary<string, object?>
            {
                ["orders"] = orders.Count,
                ["units"] = units.Count,
                ["supports"] = supports.Count,
                ["racks"] = racks.Count,
                ["sections"] = sections.Count
            })
        });

        foreach (var unit in units.Values)
        {
            await EnsureOperationalEventAsync(db, new OperationalEvent
            {
                EventCode = $"SEED-UNIT-CREATED-{unit.Id}",
                EventType = OperationalEventTypes.ProductUnitCreated,
                ProductUnitId = unit.Id,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                ToSectionId = unit.CurrentSectionId,
                SupportId = unit.CurrentSupportId,
                Source = OperationalEventSources.Seed,
                OccurredAt = unit.CreatedAt,
                Notes = $"Unidade {unit.UnitCode} criada nos dados demonstrativos.",
                IsDemo = true
            });

            if (unit.CurrentSupportId.HasValue)
            {
                await EnsureOperationalEventAsync(db, new OperationalEvent
                {
                    EventCode = $"SEED-SUPPORT-ASSIGNED-{unit.Id}-{unit.CurrentSupportId.Value}",
                    EventType = OperationalEventTypes.SupportAssigned,
                    ProductUnitId = unit.Id,
                    SupportId = unit.CurrentSupportId,
                    ManufacturingOrderId = unit.ManufacturingOrderId,
                    ToSectionId = unit.CurrentSectionId,
                    Source = OperationalEventSources.Seed,
                    OccurredAt = unit.CreatedAt.AddMinutes(5),
                    Notes = "Suporte associado à unidade demonstrativa.",
                    IsDemo = true
                });
            }
        }

        var seedMovements = await db.ProductUnitLocationHistory
            .AsNoTracking()
            .Where(x => x.Source == SeedSource)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();
        foreach (var movement in seedMovements)
        {
            await EnsureOperationalEventAsync(db, new OperationalEvent
            {
                EventCode = $"SEED-MOVEMENT-{movement.Id}",
                EventType = MapMovementEventType(movement.EventType),
                ProductUnitId = movement.ProductUnitId,
                SupportId = movement.ToSupportId ?? movement.FromSupportId,
                ManufacturingOrderId = units.Values.FirstOrDefault(x => x.Id == movement.ProductUnitId)?.ManufacturingOrderId,
                FromProductionLineId = movement.FromProductionLineId,
                ToProductionLineId = movement.ToProductionLineId,
                FromSectionId = movement.FromSectionId,
                ToSectionId = movement.ToSectionId,
                RackId = movement.EventType.Equals("TransferToRack", StringComparison.OrdinalIgnoreCase) ? racks.Values.FirstOrDefault(x => x.SectionId == movement.ToSectionId)?.Id : null,
                ReasonCode = movement.EventType,
                Source = OperationalEventSources.Seed,
                OccurredAt = movement.OccurredAt,
                Notes = movement.Reason,
                IsDemo = true,
                MetadataJson = SerializeMetadata(new Dictionary<string, object?>
                {
                    ["movementCorrelationId"] = movement.CorrelationId,
                    ["source"] = movement.Source
                })
            });
        }

        foreach (var item in quality.Values)
        {
            var unit = units.Values.First(x => x.Id == item.ProductUnitId);
            await EnsureOperationalEventAsync(db, new OperationalEvent
            {
                EventCode = $"SEED-QUALITY-{item.Id}",
                EventType = OperationalEventTypes.QualityRecorded,
                ProductUnitId = item.ProductUnitId,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                CheckpointId = item.CheckpointId,
                QualityResultId = item.Id,
                ReasonCode = item.Result,
                Severity = item.Result.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "Maior" : null,
                Source = OperationalEventSources.Seed,
                OccurredAt = item.RecordedAt,
                Notes = item.Notes,
                IsDemo = true
            });
        }

        foreach (var item in nonconformities.Values)
        {
            var unit = units.Values.First(x => x.Id == item.ProductUnitId);
            await EnsureOperationalEventAsync(db, new OperationalEvent
            {
                EventCode = $"SEED-NC-{item.Id}",
                EventType = OperationalEventTypes.NonconformityOpened,
                ProductUnitId = item.ProductUnitId,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                QualityResultId = item.QualityResultId,
                NonconformityId = item.Id,
                Severity = item.Severity,
                Source = OperationalEventSources.Seed,
                OccurredAt = item.CreatedAt,
                Notes = item.Description,
                IsDemo = true
            });
        }

        var unitIds = units.Values.Select(x => x.Id).ToArray();
        var reworkRecords = await db.ReworkRecords.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).ToListAsync();
        foreach (var item in reworkRecords)
        {
            var unit = units.Values.First(x => x.Id == item.ProductUnitId);
            await EnsureOperationalEventAsync(db, new OperationalEvent
            {
                EventCode = $"SEED-REWORK-{item.Id}",
                EventType = item.EndedAt.HasValue ? OperationalEventTypes.ReworkCompleted : OperationalEventTypes.ReworkStarted,
                ProductUnitId = item.ProductUnitId,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                NonconformityId = item.NonconformityId,
                ReworkRecordId = item.Id,
                Source = OperationalEventSources.Seed,
                OccurredAt = item.EndedAt ?? item.StartedAt,
                Notes = item.Notes,
                IsDemo = true
            });
        }

        var scrapRecords = await db.ScrapRecords.AsNoTracking().Where(x => unitIds.Contains(x.ProductUnitId)).ToListAsync();
        foreach (var item in scrapRecords)
        {
            var unit = units.Values.First(x => x.Id == item.ProductUnitId);
            await EnsureOperationalEventAsync(db, new OperationalEvent
            {
                EventCode = $"SEED-SCRAP-{item.Id}",
                EventType = OperationalEventTypes.ScrapRecorded,
                ProductUnitId = item.ProductUnitId,
                ManufacturingOrderId = unit.ManufacturingOrderId,
                NonconformityId = item.NonconformityId,
                ScrapRecordId = item.Id,
                Source = OperationalEventSources.Seed,
                OccurredAt = item.ScrappedAt,
                Notes = item.Reason,
                IsDemo = true
            });
        }
    }

    private static async Task SeedPredictionsAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, ManufacturingOrder> orders)
    {
        await EnsurePredictionAsync(db, orders["of1"], "previsao-v1", "Tempo de conclusão (demo)", now);
        await EnsurePredictionAsync(db, orders["of2"], "previsao-v1", "Risco de atraso (demo)", now.AddMinutes(-25));
        await EnsurePredictionAsync(db, orders["of3"], "previsao-v1", "Risco de qualidade (demo)", now.AddMinutes(-45));
    }

    private static async Task NormalizeLegacyDemoCodesAsync(DriveTraceDbContext db)
    {
        await RenameProductionLineAsync(db, "DL-01", "LINHA-01");
        await RenameProductionLineAsync(db, "DL-02", "LINHA-02");
        await RenameProductionLineAsync(db, "DL-03", "LINHA-03");
        await RenameProductionLineAsync(db, "DL-04", "LINHA-04");

        await RenameSectionAsync(db, "SEC-RAW", "SEC-MP");
        await RenameSectionAsync(db, "SEC-SUPPORT", "SEC-ATRIB-SUP");
        await RenameSectionAsync(db, "SEC-STAMP", "SEC-CORTE-ESTAMP");
        await RenameSectionAsync(db, "SEC-WELD", "SEC-SOLD");
        await RenameSectionAsync(db, "SEC-PAINT", "SEC-TRANS-PINT");
        await RenameSectionAsync(db, "SEC-QC", "SEC-CQ");
        await RenameSectionAsync(db, "SEC-PAINT-PREP-A", "SEC-PREP-PINT-A");
        await RenameSectionAsync(db, "SEC-PAINT-A", "SEC-PINT-A");
        await RenameSectionAsync(db, "SEC-CURE-A", "SEC-CURA-A");
        await RenameSectionAsync(db, "SEC-PAINT-B", "SEC-PINT-B");
        await RenameSectionAsync(db, "SEC-PAINT-INSPECT-B", "SEC-INSPEC-PINT-B");
        await RenameSectionAsync(db, "SEC-FINAL-ASSY", "SEC-MONT-FINAL");
        await RenameSectionAsync(db, "SEC-REWORK-CELL", "SEC-RETRAB");
        await RenameSectionAsync(db, "SEC-EXPEDITION", "SEC-EXPED");

        await RenameOrderAsync(db, "MO-DRIVE-DOOR-001", "OF-PORTA-001");
        await RenameCustomerAsync(db, "CUST-DEMO", "CLI-AUTO-001");

        await RenameCheckpointAsync(db, "CP-STAMP-01", "PC-ESTAMP-001");
        await RenameCheckpointAsync(db, "CP-WELD-01", "PC-SOLD-001");
        await RenameCheckpointAsync(db, "CP-PAINT-01", "PC-PINT-001");
        await RenameCheckpointAsync(db, "CP-QC-01", "PC-CQ-001");

        await RenameProductUnitAsync(db, "DU-001", "UP-PORTA-001");
        await RenameProductUnitAsync(db, "DU-002", "UP-PORTA-002");
        await RenameProductUnitAsync(db, "DU-003", "UP-PORTA-003");
        await RenameProductUnitAsync(db, "DU-004", "UP-PORTA-004");
        await RenameProductUnitAsync(db, "DU-005", "UP-PORTA-005");
        await RenameProductUnitAsync(db, "DU-006", "UP-PORTA-006");

        await RenameVariantAsync(db, "DOOR-STD", "PORTA-STD");
        await RenameVariantAsync(db, "DOOR-PRM", "PORTA-PRM");
        await RenameVariantAsync(db, "DOOR-REINF", "PORTA-REF");

        await RenameLotAsync(db, "ST-LOT-001", "LOTE-ACO-2026-001");
        await RenameLotAsync(db, "PAINT-LOT-001", "LOTE-PRIMARIO-2026-011");
        await RenameLotAsync(db, "PAINT-LOT-002", "LOTE-TINTA-2026-014");
        await RenameLotAsync(db, "RUBBER-LOT-001", "LOTE-SELANTE-2026-003");
        await RenameLotAsync(db, "WIRING-LOT-001", "LOTE-CABLAGEM-2026-010");
    }

    private static async Task NormalizeLegacyDemoTextsAsync(DriveTraceDbContext db)
    {
        foreach (var usage in await db.UnitMaterialLotUsages.Where(x => x.AssociationType == "Consumed").ToListAsync())
        {
            usage.AssociationType = "Consumido";
        }

        foreach (var usage in await db.UnitMaterialLotUsages.Where(x => x.AssociationType == "Reserved").ToListAsync())
        {
            usage.AssociationType = "Reservado";
        }

        foreach (var movement in await db.ProductUnitLocationHistory.ToListAsync())
        {
            movement.Reason = movement.Reason switch
            {
                "Transferred from door line paint buffer to paint line A" => "Transferência do buffer da linha de montagem para pintura A.",
                "Transferred from paint line B to final assembly" => "Transferência da pintura B para montagem final.",
                "Initial movement history snapshot for existing demo data." => "Snapshot inicial de localização para dados demonstrativos.",
                "Operational movement" => "Movimento operacional.",
                _ => movement.Reason
            };

            if (movement.Notes?.Contains("demo", StringComparison.OrdinalIgnoreCase) == true)
            {
                movement.Notes = movement.Notes.Replace("demo", "demonstração", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static async Task NormalizeAdHocLegacyDemoRowsAsync(
        DriveTraceDbContext db,
        DateTime now,
        IReadOnlyDictionary<string, Customer> customers,
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyDictionary<string, Variant> variants,
        ManufacturingProcess process,
        IReadOnlyDictionary<string, ProductionLine> lines,
        IReadOnlyDictionary<string, ProductionLineSection> sections)
    {
        await TranslateProductAsync(
            db,
            "Car Body Module",
            "Módulo de carroçaria",
            "Produto legado de demonstração mantido em português para testes de integração.");
        await TranslateProductAsync(
            db,
            "Steering Wheel",
            "Volante automóvel",
            "Componente legado de demonstração mantido em português para futuras extensões.");
        await TranslateProductAsync(
            db,
            "Seat",
            "Banco automóvel",
            "Componente interior legado mantido em português para testes de variantes.");

        await TranslateRawMaterialAsync(
            db,
            "Wiring Clip",
            "Clip de cablagem",
            "Clip usado para fixação da cablagem no interior da porta.");

        await TranslateVariantAsync(db, "INT-SPORT", "INT-DESP", "Interior desportivo");

        await NormalizeAdHocOrderAsync(
            db,
            "123123",
            "OF-PORTA-005",
            "TRC-PORTA-005",
            customers["auto"],
            products["porta"],
            variants["std"],
            process,
            lines["montagem"],
            now.Date.AddDays(7).AddHours(12),
            "Ordem de teste legada normalizada para a convenção PT-PT.");
        await NormalizeAdHocOrderAsync(
            db,
            "teste",
            "OF-PORTA-006",
            "TRC-PORTA-006",
            customers["piloto"],
            products["porta"],
            variants["lev"],
            process,
            lines["montagem"],
            now.Date.AddDays(8).AddHours(12),
            "Ordem de teste manual normalizada para dados demonstrativos em PT-PT.");

        foreach (var unit in await db.ProductUnits.Where(x => x.UnitType == "Subproduct").ToListAsync())
        {
            unit.UnitType = "Subproduto";
        }

        foreach (var unit in await db.ProductUnits.Where(x => x.UnitCode.StartsWith("DU-")).OrderBy(x => x.Id).ToListAsync())
        {
            var nextNumber = 11;
            var newCode = $"UP-PORTA-{nextNumber:000}";
            while (await db.ProductUnits.AnyAsync(x => x.UnitCode == newCode && x.Id != unit.Id))
            {
                nextNumber++;
                newCode = $"UP-PORTA-{nextNumber:000}";
            }

            unit.UnitCode = newCode;
            unit.UnitType = "Subproduto";
            unit.VariantId = variants["std"].Id;
            unit.CurrentSectionId ??= sections["mp"].Id;
        }
    }

    private static async Task TranslateProductAsync(DriveTraceDbContext db, string oldName, string newName, string info)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.Name == oldName);
        if (product is null || await db.Products.AnyAsync(x => x.Name == newName && x.Id != product.Id))
        {
            return;
        }

        product.Name = newName;
        product.Info = info;
    }

    private static async Task TranslateRawMaterialAsync(DriveTraceDbContext db, string oldName, string newName, string info)
    {
        var material = await db.RawMaterials.FirstOrDefaultAsync(x => x.Name == oldName);
        if (material is null || await db.RawMaterials.AnyAsync(x => x.Name == newName && x.Id != material.Id))
        {
            return;
        }

        material.Name = newName;
        material.Info = info;
    }

    private static async Task TranslateVariantAsync(DriveTraceDbContext db, string oldCode, string newCode, string name)
    {
        var variants = await db.Variants.Where(x => x.VariantCode == oldCode).ToListAsync();
        foreach (var variant in variants)
        {
            if (!await db.Variants.AnyAsync(x => x.ProductId == variant.ProductId && x.VariantCode == newCode && x.Id != variant.Id))
            {
                variant.VariantCode = newCode;
            }

            variant.Name = name;
        }
    }

    private static async Task NormalizeAdHocOrderAsync(
        DriveTraceDbContext db,
        string oldOrderNumber,
        string newOrderNumber,
        string trackingCode,
        Customer customer,
        Product product,
        Variant variant,
        ManufacturingProcess process,
        ProductionLine line,
        DateTime scheduledUntil,
        string observations)
    {
        var order = await db.ManufacturingOrders.FirstOrDefaultAsync(x => x.OrderNumber == oldOrderNumber);
        if (order is null)
        {
            return;
        }

        if (!await db.ManufacturingOrders.AnyAsync(x => x.OrderNumber == newOrderNumber && x.Id != order.Id))
        {
            order.OrderNumber = newOrderNumber;
        }

        if (!await db.ManufacturingOrders.AnyAsync(x => x.PublicTrackingCode == trackingCode && x.Id != order.Id))
        {
            order.PublicTrackingCode = trackingCode;
        }

        order.CustomerId = customer.Id;
        order.ProductId = product.Id;
        order.VariantId = variant.Id;
        order.ManufacturingProcessId = process.Id;
        order.ProductionLineId = line.Id;
        order.ScheduledUntil = scheduledUntil;
        order.CustomerReference = trackingCode.Replace("TRC-", "CLI-REF-");
        order.Observations = observations;
    }

    private static async Task<Product> EnsureProductAsync(DriveTraceDbContext db, string name, string info, params string[] aliases)
    {
        var names = aliases.Append(name).ToArray();
        var product = await db.Products.FirstOrDefaultAsync(x => names.Contains(x.Name));

        if (product is null)
        {
            product = new Product();
            db.Products.Add(product);
        }

        product.Name = name;
        product.Info = info;
        return product;
    }

    private static async Task<Variant> EnsureVariantAsync(DriveTraceDbContext db, Product product, string code, string name, params string[] aliases)
    {
        var codes = aliases.Append(code).ToArray();
        var variant = await db.Variants.FirstOrDefaultAsync(x => x.ProductId == product.Id && codes.Contains(x.VariantCode));
        if (variant is null)
        {
            variant = new Variant { ProductId = product.Id };
            db.Variants.Add(variant);
        }

        variant.ProductId = product.Id;
        variant.VariantCode = code;
        variant.Name = name;
        return variant;
    }

    private static async Task<ProductionLine> EnsureProductionLineAsync(
        DriveTraceDbContext db,
        string code,
        string name,
        int displayOrder,
        string visualGroup,
        params string[] aliases)
    {
        var codes = aliases.Append(code).ToArray();
        var line = await db.ProductionLines.FirstOrDefaultAsync(x => codes.Contains(x.LineCode));
        if (line is null)
        {
            line = new ProductionLine();
            db.ProductionLines.Add(line);
        }

        line.LineCode = code;
        line.Name = name;
        line.DisplayOrder = displayOrder;
        line.VisualGroup = visualGroup;
        return line;
    }

    private static async Task<ProductionLineSection> EnsureSectionAsync(
        DriveTraceDbContext db,
        ProductionLine line,
        string code,
        string name,
        string sectionType,
        int displayOrder,
        int layoutColumn,
        int layoutRow,
        string visualZone,
        bool isTransferPoint,
        bool allowsTransferIn,
        bool allowsTransferOut,
        params string[] aliases)
    {
        var codes = aliases.Append(code).ToArray();
        var section = await db.ProductionLineSections.FirstOrDefaultAsync(x => codes.Contains(x.SectionCode));
        if (section is null)
        {
            section = new ProductionLineSection();
            db.ProductionLineSections.Add(section);
        }

        section.LineId = line.Id;
        section.SectionCode = code;
        section.Name = name;
        section.SectionType = sectionType;
        section.DisplayOrder = displayOrder;
        section.LayoutColumn = layoutColumn;
        section.LayoutRow = layoutRow;
        section.VisualZone = visualZone;
        section.IsTransferPoint = isTransferPoint;
        section.AllowsLineTransferIn = allowsTransferIn;
        section.AllowsLineTransferOut = allowsTransferOut;
        return section;
    }

    private static async Task<Resource> EnsureResourceAsync(
        DriveTraceDbContext db,
        string name,
        string type,
        string function,
        params string[] aliases)
    {
        var names = aliases.Append(name).ToArray();
        var resource = await db.Resources.FirstOrDefaultAsync(x => names.Contains(x.Name));
        if (resource is null)
        {
            resource = new Resource();
            db.Resources.Add(resource);
        }

        resource.Name = name;
        resource.Type = type;
        resource.Function = function;
        return resource;
    }

    private static async Task<ManufacturingProcess> EnsureProcessAsync(DriveTraceDbContext db, Product product, string name, string info)
    {
        var process = await db.ManufacturingProcesses.FirstOrDefaultAsync(x => x.ProductId == product.Id)
            ?? await db.ManufacturingProcesses.FirstOrDefaultAsync(x => x.ProcessName == "Automotive door multi-line route" || x.ProcessName == "Processo de Fabrico de Porta");

        if (process is null)
        {
            process = new ManufacturingProcess { ProductId = product.Id };
            db.ManufacturingProcesses.Add(process);
        }

        process.ProductId = product.Id;
        process.ProcessName = name;
        process.Info = info;
        return process;
    }

    private static async Task<ManufacturingSectionPhase> EnsureSectionPhaseAsync(
        DriveTraceDbContext db,
        ProductionLineSection section,
        string phaseInfo,
        int duration)
    {
        var phase = await db.ManufacturingSectionPhases.FirstOrDefaultAsync(x => x.SectionId == section.Id);
        if (phase is null)
        {
            phase = new ManufacturingSectionPhase { SectionId = section.Id };
            db.ManufacturingSectionPhases.Add(phase);
        }

        phase.SectionId = section.Id;
        phase.PhaseInfo = phaseInfo;
        phase.PhaseDuration = duration;
        return phase;
    }

    private static async Task EnsureProcessPhaseAsync(
        DriveTraceDbContext db,
        ManufacturingProcess process,
        ManufacturingSectionPhase phase,
        int stepOrder,
        Resource? resource)
    {
        var processPhase = await db.ManufacturingProcessPhases.FirstOrDefaultAsync(x =>
            x.ManufacturingProcessId == process.Id &&
            x.ManufacturingPhaseId == phase.Id);

        if (processPhase is null)
        {
            processPhase = new ManufacturingProcessPhase
            {
                ManufacturingProcessId = process.Id,
                ManufacturingPhaseId = phase.Id
            };
            db.ManufacturingProcessPhases.Add(processPhase);
        }

        processPhase.NumberStepOrder = stepOrder;
        processPhase.ResourceId = resource?.Id;
    }

    private static async Task<Checkpoint> EnsureCheckpointAsync(
        DriveTraceDbContext db,
        string code,
        string name,
        string status,
        ProductionLineSection section,
        params string[] aliases)
    {
        var codes = aliases.Append(code).ToArray();
        var checkpoint = await db.Checkpoints.FirstOrDefaultAsync(x => codes.Contains(x.CheckpointCode));
        if (checkpoint is null)
        {
            checkpoint = new Checkpoint();
            db.Checkpoints.Add(checkpoint);
        }

        checkpoint.CheckpointCode = code;
        checkpoint.Name = name;
        checkpoint.Status = status;
        checkpoint.SectionId = section.Id;
        return checkpoint;
    }

    private static async Task<Customer> EnsureCustomerAsync(
        DriveTraceDbContext db,
        string code,
        string name,
        string email,
        params string[] aliases)
    {
        var codes = aliases.Append(code).ToArray();
        var customer = await db.Customers.FirstOrDefaultAsync(x => codes.Contains(x.CustomerCode));
        if (customer is null)
        {
            customer = new Customer();
            db.Customers.Add(customer);
        }

        customer.CustomerCode = code;
        customer.Name = name;
        customer.ContactEmail = email;
        customer.IsActive = true;
        return customer;
    }

    private static async Task<ManufacturingOrder> EnsureOrderAsync(
        DriveTraceDbContext db,
        string orderNumber,
        Customer customer,
        Product product,
        Variant variant,
        ManufacturingProcess process,
        ProductionLine line,
        int plannedQty,
        DateTime scheduledUntil,
        string status,
        string customerReference,
        string trackingCode,
        string observations,
        string? legacyOrderNumber = null,
        string? legacyTrackingCode = null)
    {
        var order = await db.ManufacturingOrders.FirstOrDefaultAsync(x =>
            x.OrderNumber == orderNumber ||
            (legacyOrderNumber != null && x.OrderNumber == legacyOrderNumber) ||
            x.PublicTrackingCode == trackingCode ||
            (legacyTrackingCode != null && x.PublicTrackingCode == legacyTrackingCode));

        if (order is null)
        {
            order = new ManufacturingOrder();
            db.ManufacturingOrders.Add(order);
        }

        order.OrderNumber = orderNumber;
        order.CustomerId = customer.Id;
        order.ProductId = product.Id;
        order.VariantId = variant.Id;
        order.ManufacturingProcessId = process.Id;
        order.ProductionLineId = line.Id;
        order.PlannedQty = plannedQty;
        order.ScheduledUntil = scheduledUntil;
        order.Status = status;
        order.CustomerReference = customerReference;
        order.PublicTrackingCode = trackingCode;
        order.Observations = observations;
        return order;
    }

    private static async Task<Support> EnsureSupportAsync(
        DriveTraceDbContext db,
        string code,
        string status,
        ProductionLineSection section)
    {
        var support = await db.Supports.FirstOrDefaultAsync(x => x.SupportCode == code);
        if (support is null)
        {
            support = new Support { SupportCode = code };
            db.Supports.Add(support);
        }

        support.Status = status;
        support.CurrentSectionId = section.Id;
        return support;
    }

    private static async Task<Rack> EnsureRackAsync(
        DriveTraceDbContext db,
        string code,
        string status,
        ProductionLineSection section)
    {
        var rack = await db.Racks.FirstOrDefaultAsync(x => x.RackCode == code);
        if (rack is null)
        {
            rack = new Rack { RackCode = code };
            db.Racks.Add(rack);
        }

        rack.Status = status;
        rack.SectionId = section.Id;
        return rack;
    }

    private static async Task<ProductUnit> EnsureProductUnitAsync(
        DriveTraceDbContext db,
        ManufacturingOrder order,
        Variant variant,
        string code,
        string unitType,
        string status,
        string qualityStatus,
        ProductionLineSection section,
        Support? support,
        DateTime createdAt,
        DateTime? completedAt,
        params string[] aliases)
    {
        var codes = aliases.Append(code).ToArray();
        var unit = await db.ProductUnits.FirstOrDefaultAsync(x => codes.Contains(x.UnitCode));
        if (unit is null)
        {
            unit = new ProductUnit { CreatedAt = createdAt };
            db.ProductUnits.Add(unit);
        }

        unit.ManufacturingOrderId = order.Id;
        unit.VariantId = variant.Id;
        unit.UnitCode = code;
        unit.UnitType = unitType;
        unit.Status = status;
        unit.QualityStatus = qualityStatus;
        unit.CurrentSectionId = section.Id;
        unit.CurrentSupportId = support?.Id;
        unit.CreatedAt = unit.CreatedAt == default ? createdAt : unit.CreatedAt;
        unit.CompletedAt = completedAt;
        return unit;
    }

    private static async Task EnsureOpenSupportAssignmentAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        Support support,
        DateTime dateTimeIn)
    {
        var openAssignments = await db.UnitSupportAssignments
            .Where(x => x.ProductUnitId == unit.Id && x.DateTimeOut == null)
            .ToListAsync();

        foreach (var assignment in openAssignments.Where(x => x.SupportId != support.Id))
        {
            assignment.DateTimeOut = dateTimeIn;
        }

        if (!openAssignments.Any(x => x.SupportId == support.Id))
        {
            db.UnitSupportAssignments.Add(new UnitSupportAssignment
            {
                ProductUnitId = unit.Id,
                SupportId = support.Id,
                DateTimeIn = dateTimeIn
            });
        }
    }

    private static async Task EnsureSupportLocalizationAsync(
        DriveTraceDbContext db,
        Support support,
        ProductionLineSection section,
        string eventType,
        DateTime dateTime)
    {
        var exists = await db.SupportLocalizationHistory.AnyAsync(x =>
            x.SupportId == support.Id &&
            x.SectionId == section.Id &&
            x.EventType == eventType);

        if (!exists)
        {
            db.SupportLocalizationHistory.Add(new SupportLocalizationHistory
            {
                SupportId = support.Id,
                SectionId = section.Id,
                EventType = eventType,
                DateTime = dateTime
            });
        }
    }

    private static async Task EnsureProductTransferHistoryAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        ProductionLineSection? fromSection,
        ProductionLineSection toSection,
        Support? fromSupport,
        Support? toSupport,
        string eventType,
        string reason,
        DateTime occurredAt)
    {
        var correlationId = $"seed-ptpt-{unit.UnitCode}-{toSection.SectionCode}-{eventType}";
        var movement = await db.ProductUnitLocationHistory.FirstOrDefaultAsync(x => x.CorrelationId == correlationId);

        if (movement is null)
        {
            movement = new ProductUnitLocationHistory
            {
                ProductUnitId = unit.Id,
                CorrelationId = correlationId
            };
            db.ProductUnitLocationHistory.Add(movement);
        }

        movement.ProductUnitId = unit.Id;
        movement.FromProductionLineId = fromSection?.LineId;
        movement.ToProductionLineId = toSection.LineId;
        movement.FromSectionId = fromSection?.Id;
        movement.ToSectionId = toSection.Id;
        movement.FromSupportId = fromSupport?.Id;
        movement.ToSupportId = toSupport?.Id;
        movement.EventType = eventType;
        movement.Reason = reason;
        movement.OccurredAt = occurredAt;
        movement.Source = SeedSource;
    }

    private static async Task EnsureProductLocationSnapshotAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        ProductionLineSection section,
        Support? support,
        DateTime occurredAt)
    {
        var correlationId = $"seed-ptpt-{unit.UnitCode}-{section.SectionCode}-snapshot";
        var exists = await db.ProductUnitLocationHistory.AnyAsync(x => x.CorrelationId == correlationId);
        if (exists) return;

        db.ProductUnitLocationHistory.Add(new ProductUnitLocationHistory
        {
            ProductUnitId = unit.Id,
            ToProductionLineId = section.LineId,
            ToSectionId = section.Id,
            ToSupportId = support?.Id,
            EventType = "SeededCurrentLocation",
            Reason = "Snapshot atual da localização demonstrativa.",
            OccurredAt = occurredAt,
            Source = SeedSource,
            CorrelationId = correlationId
        });
    }

    private static async Task EnsureOperationalEventAsync(DriveTraceDbContext db, OperationalEvent value)
    {
        var item = await db.OperationalEvents.FirstOrDefaultAsync(x => x.EventCode == value.EventCode);
        if (item is null)
        {
            item = new OperationalEvent { EventCode = value.EventCode };
            db.OperationalEvents.Add(item);
        }

        item.EventType = value.EventType;
        item.ProductUnitId = value.ProductUnitId;
        item.SupportId = value.SupportId;
        item.ManufacturingOrderId = value.ManufacturingOrderId;
        item.FromProductionLineId = value.FromProductionLineId;
        item.ToProductionLineId = value.ToProductionLineId;
        item.FromSectionId = value.FromSectionId;
        item.ToSectionId = value.ToSectionId;
        item.CheckpointId = value.CheckpointId;
        item.QualityResultId = value.QualityResultId;
        item.NonconformityId = value.NonconformityId;
        item.ReworkRecordId = value.ReworkRecordId;
        item.ScrapRecordId = value.ScrapRecordId;
        item.RackId = value.RackId;
        item.ReasonCode = value.ReasonCode;
        item.Severity = value.Severity;
        item.Source = value.Source;
        item.PerformedByUserId = value.PerformedByUserId;
        item.OccurredAt = value.OccurredAt;
        item.Notes = value.Notes;
        item.IsDemo = value.IsDemo;
        item.MetadataJson = value.MetadataJson;
    }

    private static string MapMovementEventType(string movementEventType)
    {
        return movementEventType switch
        {
            "SupportAssigned" => OperationalEventTypes.SupportAssigned,
            "LineTransfer" => OperationalEventTypes.LineTransfer,
            "TransferToRack" => OperationalEventTypes.RackAssigned,
            _ => OperationalEventTypes.SectionMovement
        };
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?>? metadata)
    {
        return metadata is null ? null : JsonSerializer.Serialize(metadata, OperationalEventJsonOptions);
    }

    private static async Task EnsureRackAssignmentAsync(
        DriveTraceDbContext db,
        Rack rack,
        Support support,
        DateTime dateTimeIn)
    {
        var exists = await db.RackSupportAssignments.AnyAsync(x =>
            x.RackId == rack.Id &&
            x.SupportId == support.Id &&
            x.DateTimeOut == null);

        if (!exists)
        {
            db.RackSupportAssignments.Add(new RackSupportAssignment
            {
                RackId = rack.Id,
                SupportId = support.Id,
                DateTimeIn = dateTimeIn
            });
        }
    }

    private static async Task<RawMaterial> EnsureRawMaterialAsync(
        DriveTraceDbContext db,
        string name,
        string info,
        params string[] aliases)
    {
        var names = aliases.Append(name).ToArray();
        var material = await db.RawMaterials.FirstOrDefaultAsync(x => names.Contains(x.Name));
        if (material is null)
        {
            material = new RawMaterial();
            db.RawMaterials.Add(material);
        }

        material.Name = name;
        material.Info = info;
        return material;
    }

    private static async Task<LotRawMaterial> EnsureLotAsync(
        DriveTraceDbContext db,
        RawMaterial material,
        ProductionLineSection section,
        string lotNumber,
        int quantity,
        string unit,
        params string[] aliases)
    {
        var lots = aliases.Append(lotNumber).ToArray();
        var lot = await db.LotRawMaterials.FirstOrDefaultAsync(x => lots.Contains(x.LotNumber));
        if (lot is null)
        {
            lot = new LotRawMaterial();
            db.LotRawMaterials.Add(lot);
        }

        lot.RawMaterialId = material.Id;
        lot.SectionId = section.Id;
        lot.LotNumber = lotNumber;
        lot.LotQuantity = quantity;
        lot.LotUnit = unit;
        return lot;
    }

    private static async Task EnsureMaterialUsageAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        LotRawMaterial lot,
        string associationType,
        int quantity)
    {
        var usage = await db.UnitMaterialLotUsages.FirstOrDefaultAsync(x =>
            x.ProductUnitId == unit.Id &&
            x.LotId == lot.Id &&
            x.AssociationType == associationType);

        if (usage is null)
        {
            usage = new UnitMaterialLotUsage
            {
                ProductUnitId = unit.Id,
                LotId = lot.Id
            };
            db.UnitMaterialLotUsages.Add(usage);
        }

        usage.AssociationType = associationType;
        usage.Quantity = quantity;
    }

    private static async Task<QualityResult> EnsureQualityResultAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        Checkpoint checkpoint,
        string result,
        DateTime recordedAt,
        string notes)
    {
        var quality = await db.QualityResults.FirstOrDefaultAsync(x =>
            x.ProductUnitId == unit.Id &&
            x.CheckpointId == checkpoint.Id &&
            x.Result == result);

        if (quality is null)
        {
            quality = new QualityResult
            {
                ProductUnitId = unit.Id,
                CheckpointId = checkpoint.Id,
                Result = result
            };
            db.QualityResults.Add(quality);
        }

        quality.RecordedAt = recordedAt;
        quality.Notes = notes;
        return quality;
    }

    private static async Task<Nonconformity> EnsureNonconformityAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        QualityResult quality,
        string severity,
        string status,
        string description,
        DateTime createdAt)
    {
        var nonconformity = await db.Nonconformities.FirstOrDefaultAsync(x =>
            x.ProductUnitId == unit.Id &&
            x.QualityResultId == quality.Id);

        if (nonconformity is null)
        {
            nonconformity = new Nonconformity
            {
                ProductUnitId = unit.Id,
                QualityResultId = quality.Id
            };
            db.Nonconformities.Add(nonconformity);
        }

        nonconformity.Severity = severity;
        nonconformity.Status = status;
        nonconformity.Description = description;
        nonconformity.CreatedAt = createdAt;
        return nonconformity;
    }

    private static async Task EnsureReworkRecordAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        Nonconformity nonconformity,
        DateTime startedAt,
        string status,
        string notes)
    {
        var rework = await db.ReworkRecords.FirstOrDefaultAsync(x => x.ProductUnitId == unit.Id);
        if (rework is null)
        {
            rework = new ReworkRecord { ProductUnitId = unit.Id };
            db.ReworkRecords.Add(rework);
        }

        rework.NonconformityId = nonconformity.Id;
        rework.StartedAt = startedAt;
        rework.Status = status;
        rework.Notes = notes;
    }

    private static async Task EnsureScrapRecordAsync(
        DriveTraceDbContext db,
        ProductUnit unit,
        Nonconformity nonconformity,
        DateTime scrappedAt,
        string reason)
    {
        var scrap = await db.ScrapRecords.FirstOrDefaultAsync(x => x.ProductUnitId == unit.Id);
        if (scrap is null)
        {
            scrap = new ScrapRecord { ProductUnitId = unit.Id };
            db.ScrapRecords.Add(scrap);
        }

        scrap.NonconformityId = nonconformity.Id;
        scrap.ScrappedAt = scrappedAt;
        scrap.Reason = reason;
    }

    private static async Task EnsurePredictionAsync(
        DriveTraceDbContext db,
        ManufacturingOrder order,
        string modelVersion,
        string modelType,
        DateTime lastDate)
    {
        var prediction = await db.Predictions.FirstOrDefaultAsync(x => x.ManufacturingOrderId == order.Id && x.ModelType == modelType);
        if (prediction is null)
        {
            prediction = new Prediction { ManufacturingOrderId = order.Id, CreatedAt = lastDate };
            db.Predictions.Add(prediction);
        }

        prediction.ModelVersion = modelVersion;
        prediction.ModelType = modelType;
        prediction.LastDate = lastDate;
    }

    private static async Task RenameProductionLineAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == oldCode);
        if (item is not null && !await db.ProductionLines.AnyAsync(x => x.LineCode == newCode && x.Id != item.Id))
        {
            item.LineCode = newCode;
        }
    }

    private static async Task RenameSectionAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.ProductionLineSections.FirstOrDefaultAsync(x => x.SectionCode == oldCode);
        if (item is not null && !await db.ProductionLineSections.AnyAsync(x => x.SectionCode == newCode && x.Id != item.Id))
        {
            item.SectionCode = newCode;
        }
    }

    private static async Task RenameOrderAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.ManufacturingOrders.FirstOrDefaultAsync(x => x.OrderNumber == oldCode);
        if (item is not null && !await db.ManufacturingOrders.AnyAsync(x => x.OrderNumber == newCode && x.Id != item.Id))
        {
            item.OrderNumber = newCode;
        }
    }

    private static async Task RenameCustomerAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.Customers.FirstOrDefaultAsync(x => x.CustomerCode == oldCode);
        if (item is not null && !await db.Customers.AnyAsync(x => x.CustomerCode == newCode && x.Id != item.Id))
        {
            item.CustomerCode = newCode;
        }
    }

    private static async Task RenameCheckpointAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.Checkpoints.FirstOrDefaultAsync(x => x.CheckpointCode == oldCode);
        if (item is not null && !await db.Checkpoints.AnyAsync(x => x.CheckpointCode == newCode && x.Id != item.Id))
        {
            item.CheckpointCode = newCode;
        }
    }

    private static async Task RenameProductUnitAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.ProductUnits.FirstOrDefaultAsync(x => x.UnitCode == oldCode);
        if (item is not null && !await db.ProductUnits.AnyAsync(x => x.UnitCode == newCode && x.Id != item.Id))
        {
            item.UnitCode = newCode;
        }
    }

    private static async Task RenameVariantAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var items = await db.Variants.Where(x => x.VariantCode == oldCode).ToListAsync();
        foreach (var item in items)
        {
            if (!await db.Variants.AnyAsync(x => x.ProductId == item.ProductId && x.VariantCode == newCode && x.Id != item.Id))
            {
                item.VariantCode = newCode;
            }
        }
    }

    private static async Task RenameLotAsync(DriveTraceDbContext db, string oldCode, string newCode)
    {
        var item = await db.LotRawMaterials.FirstOrDefaultAsync(x => x.LotNumber == oldCode);
        if (item is not null && !await db.LotRawMaterials.AnyAsync(x => x.LotNumber == newCode && x.Id != item.Id))
        {
            item.LotNumber = newCode;
        }
    }
}
