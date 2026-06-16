using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveTraceCore.Api.Services;

public static class SimulationRunStatuses
{
    public const string Draft = "Draft";
    public const string Running = "Running";
    public const string Paused = "Paused";
    public const string Completed = "Completed";
    public const string Stopped = "Stopped";
}

public static class SimulationExecutionModes
{
    public const string Manual = "Manual";
    public const string Automatic = "Automatic";
}

public sealed record SimulationScenarioDefinition(
    string Key,
    string Name,
    string Description,
    int StepCount,
    string Outcome,
    string DefaultUnitCode,
    string DefaultOrderNumber,
    string SupportCode,
    string RackCode,
    IReadOnlyList<string> Entities,
    IReadOnlyList<string> Highlights);

internal sealed record SimulationAssets(
    ManufacturingOrder Order,
    ProductUnit Unit,
    Support Support,
    Rack Rack,
    ProductionLineSection RawMaterials,
    ProductionLineSection SupportAssignment,
    ProductionLineSection Cutting,
    ProductionLineSection Welding,
    ProductionLineSection Painting,
    ProductionLineSection Quality,
    ProductionLineSection Rework,
    ProductionLineSection RackSection,
    Checkpoint QualityCheckpoint);

internal sealed record SimulationStepOutcome(
    string StepType,
    string Description,
    string Result,
    int? ProductUnitId,
    int? ManufacturingOrderId,
    int? FromLineId,
    int? ToLineId,
    int? FromSectionId,
    int? ToSectionId);

public sealed class ProductionSimulationService
{
    private static readonly IReadOnlyDictionary<string, SimulationScenarioDefinition> ScenarioDefinitions =
        new Dictionary<string, SimulationScenarioDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["normal-flow"] = new(
                "normal-flow",
                "Fluxo normal",
                "Uma unidade percorre a linha, recebe suporte, passa por corte, soldadura, pintura, qualidade e termina em rack conforme.",
                6,
                "Unidade conforme e rastreabilidade completa no grafo.",
                "UP-SIM-001",
                "OF-SIM-PORTA-001",
                "SUP-SIM-001",
                "RACK-SIM-001",
                ["Ordem, unidade, suporte, rack, qualidade e eventos operacionais."],
                ["Sem não conformidade", "Rota conforme", "Fecho em rack"]),
            ["minor-recovery"] = new(
                "minor-recovery",
                "Falha menor e recondicionamento",
                "Uma unidade sofre uma falha menor na pintura, vai para retrabalho, é recuperada, recondicionada e termina em rack.",
                9,
                "Não conformidade menor, retrabalho concluído e unidade recondicionada.",
                "UP-SIM-002",
                "OF-SIM-PORTA-002",
                "SUP-SIM-002",
                "RACK-SIM-002",
                ["Ordem, unidade, suporte, rack, qualidade, retrabalho, recondicionamento e eventos operacionais."],
                ["Falha menor", "Retrabalho", "Recondicionamento", "Rastreamento completo"]),
            ["critical-scrap"] = new(
                "critical-scrap",
                "Falha crítica e sucata",
                "Uma unidade recebe uma não conformidade crítica e termina em sucata, sem entrar no fluxo de recuperação.",
                6,
                "Não conformidade crítica e descarte em sucata.",
                "UP-SIM-003",
                "OF-SIM-PORTA-003",
                "SUP-SIM-003",
                "RACK-SIM-003",
                ["Ordem, unidade, suporte, rack, qualidade, sucata e eventos operacionais."],
                ["Falha crítica", "Sem recuperação", "Sucata final"])
        };

    private readonly DriveTraceDbContext _db;
    private readonly ProductionFlowService _flow;
    private readonly ReconditioningService _reconditioning;
    private readonly DemoEventService _demoEvents;
    private readonly OperationalEventService _events;

    public ProductionSimulationService(
        DriveTraceDbContext db,
        ProductionFlowService flow,
        ReconditioningService reconditioning,
        DemoEventService demoEvents,
        OperationalEventService events)
    {
        _db = db;
        _flow = flow;
        _reconditioning = reconditioning;
        _demoEvents = demoEvents;
        _events = events;
    }

    public Task<IReadOnlyList<SimulationScenarioDto>> GetScenariosAsync(CancellationToken cancellationToken = default)
    {
        var items = ScenarioDefinitions.Values.Select(ToDto).OrderBy(x => x.Name).ToList();
        return Task.FromResult<IReadOnlyList<SimulationScenarioDto>>(items);
    }

    public async Task<SimulationStateDto> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _db.SimulationRuns.AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var summaries = await BuildSummariesAsync(runs, cancellationToken);
        var activeRuns = summaries.Where(x => IsActive(x.Status)).ToList();
        var alerts = new List<string>();
        if (activeRuns.Any(x => x.Status.Equals(SimulationRunStatuses.Paused, StringComparison.OrdinalIgnoreCase)))
        {
            alerts.Add("Existe pelo menos uma simulação em pausa.");
        }
        if (!activeRuns.Any())
        {
            alerts.Add("Não existem simulações ativas neste momento.");
        }

        return new SimulationStateDto
        {
            GeneratedAt = DateTime.UtcNow,
            LastAction = summaries.FirstOrDefault()?.LastStepDescription ?? "Sem ações recentes.",
            UnitsInMotion = activeRuns.Count(x => x.Status.Equals(SimulationRunStatuses.Running, StringComparison.OrdinalIgnoreCase)),
            Alerts = alerts,
            Scenarios = ScenarioDefinitions.Values.Select(ToDto).OrderBy(x => x.Name).ToList(),
            ActiveRuns = activeRuns,
            RecentRuns = summaries
        };
    }

    public async Task<IReadOnlyList<SimulationRunSummaryDto>> GetRunsAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _db.SimulationRuns.AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        return await BuildSummariesAsync(runs, cancellationToken);
    }

    public async Task<SimulationRunDetailDto?> GetRunAsync(int id, CancellationToken cancellationToken = default)
    {
        var run = await _db.SimulationRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (run is null) return null;

        return await BuildDetailAsync(run, cancellationToken);
    }

    public async Task<SimulationRunDetailDto> CreateRunAsync(
        SimulationRunCreateRequest request,
        string? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioKey))
        {
            throw new InvalidOperationException("É obrigatório indicar um cenário de simulação.");
        }

        var scenario = ResolveScenario(request.ScenarioKey);
        var executionMode = NormalizeMode(request.Speed);
        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await EnsureNoActiveRunForScenarioAsync(scenario.Key, cancellationToken);
        var assets = await LoadScenarioAssetsAsync(scenario, cancellationToken);
        await InitializeScenarioAssetsAsync(assets, now, cancellationToken);

        var run = new SimulationRun
        {
            RunCode = await GenerateRunCodeAsync(scenario.Key, cancellationToken),
            Name = string.IsNullOrWhiteSpace(request.Name) ? scenario.Name : request.Name.Trim(),
            ScenarioKey = scenario.Key,
            Status = SimulationRunStatuses.Running,
            ExecutionMode = executionMode,
            StartedAt = now,
            CurrentStep = 0,
            CreatedByUserId = createdByUserId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        _db.SimulationRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-CREATED-{run.RunCode}",
            EventType = OperationalEventTypes.SimulationRunCreated,
            ProductUnitId = assets.Unit.Id,
            ManufacturingOrderId = assets.Order.Id,
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = createdByUserId,
            OccurredAt = now,
            Notes = $"Simulação '{run.Name}' criada para o cenário {scenario.Name}.",
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["scenarioKey"] = scenario.Key,
                ["runCode"] = run.RunCode,
                ["executionMode"] = executionMode,
                ["stepCount"] = scenario.StepCount
            }
        }, saveChanges: false, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildDetailByIdAsync(run.Id, cancellationToken);
    }

    public async Task<SimulationRunDetailDto> TickAsync(
        int runId,
        string? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        var run = await LoadRunAsync(runId, cancellationToken);
        var scenario = ResolveScenario(run.ScenarioKey);
        if (!run.Status.Equals(SimulationRunStatuses.Running, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A simulação tem de estar em execução para avançar um passo.");
        }

        if (run.CurrentStep >= scenario.StepCount)
        {
            run.Status = SimulationRunStatuses.Completed;
            run.CompletedAt ??= DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return await BuildDetailAsync(run, cancellationToken);
        }

        var assets = await LoadScenarioAssetsAsync(scenario, cancellationToken);
        var stepNumber = run.CurrentStep + 1;
        var now = DateTime.UtcNow;
        var outcome = await ExecuteStepAsync(run, scenario, assets, stepNumber, performedByUserId, now, cancellationToken);
        run.CurrentStep = stepNumber;

        var genericEvent = await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-STEP-{stepNumber}",
            EventType = OperationalEventTypes.SimulationStepExecuted,
            ProductUnitId = outcome.ProductUnitId,
            ManufacturingOrderId = outcome.ManufacturingOrderId,
            FromProductionLineId = outcome.FromLineId,
            ToProductionLineId = outcome.ToLineId,
            FromSectionId = outcome.FromSectionId,
            ToSectionId = outcome.ToSectionId,
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = outcome.Description,
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["scenarioKey"] = scenario.Key,
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode,
                ["stepNumber"] = stepNumber,
                ["stepType"] = outcome.StepType,
                ["result"] = outcome.Result
            }
        }, saveChanges: true, cancellationToken);

        _db.SimulationSteps.Add(new SimulationStep
        {
            SimulationRunId = run.Id,
            StepNumber = stepNumber,
            StepType = outcome.StepType,
            Description = outcome.Description,
            ExecutedAt = now,
            ProductUnitId = outcome.ProductUnitId,
            ManufacturingOrderId = outcome.ManufacturingOrderId,
            FromLineId = outcome.FromLineId,
            ToLineId = outcome.ToLineId,
            FromSectionId = outcome.FromSectionId,
            ToSectionId = outcome.ToSectionId,
            Result = outcome.Result,
            OperationalEventId = genericEvent.Id
        });

        if (run.CurrentStep >= scenario.StepCount)
        {
            run.Status = SimulationRunStatuses.Completed;
            run.CompletedAt = now;
            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"SIM-{run.RunCode}-COMPLETED",
                EventType = OperationalEventTypes.SimulationCompleted,
                ProductUnitId = outcome.ProductUnitId,
                ManufacturingOrderId = outcome.ManufacturingOrderId,
                Source = OperationalEventSources.Simulation,
                PerformedByUserId = performedByUserId,
                OccurredAt = now.AddMilliseconds(1),
                Notes = $"Simulação '{run.Name}' concluída.",
                IsDemo = true,
                Metadata = new Dictionary<string, object?>
                {
                    ["scenarioKey"] = scenario.Key,
                    ["runId"] = run.Id,
                    ["runCode"] = run.RunCode
                }
            }, saveChanges: false, cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildDetailAsync(run, cancellationToken);
    }

    public Task<SimulationRunDetailDto> RunStepAsync(int runId, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        return TickAsync(runId, performedByUserId, cancellationToken);
    }

    public async Task<SimulationRunDetailDto> PauseAsync(int runId, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunAsync(runId, cancellationToken);
        if (!run.Status.Equals(SimulationRunStatuses.Running, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Só é possível pausar uma simulação em execução.");
        }

        var now = DateTime.UtcNow;
        run.Status = SimulationRunStatuses.Paused;
        run.PausedAt = now;
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-PAUSE-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationPaused,
            ProductUnitId = await GetRunUnitIdAsync(run, cancellationToken),
            ManufacturingOrderId = await GetRunOrderIdAsync(run, cancellationToken),
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = $"Simulação '{run.Name}' pausada.",
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["scenarioKey"] = run.ScenarioKey,
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode
            }
        }, cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildDetailByIdAsync(run.Id, cancellationToken);
    }

    public async Task<SimulationRunDetailDto> ResumeAsync(int runId, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunAsync(runId, cancellationToken);
        if (!run.Status.Equals(SimulationRunStatuses.Paused, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Só é possível retomar uma simulação em pausa.");
        }

        var now = DateTime.UtcNow;
        run.Status = SimulationRunStatuses.Running;
        run.PausedAt = null;
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-RESUME-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationResumed,
            ProductUnitId = await GetRunUnitIdAsync(run, cancellationToken),
            ManufacturingOrderId = await GetRunOrderIdAsync(run, cancellationToken),
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = $"Simulação '{run.Name}' retomada.",
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["scenarioKey"] = run.ScenarioKey,
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode
            }
        }, cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildDetailByIdAsync(run.Id, cancellationToken);
    }

    public async Task<SimulationRunDetailDto> StopAsync(int runId, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunAsync(runId, cancellationToken);
        if (run.Status.Equals(SimulationRunStatuses.Completed, StringComparison.OrdinalIgnoreCase)
            || run.Status.Equals(SimulationRunStatuses.Stopped, StringComparison.OrdinalIgnoreCase))
        {
            return await BuildDetailByIdAsync(run.Id, cancellationToken);
        }

        var now = DateTime.UtcNow;
        run.Status = SimulationRunStatuses.Stopped;
        run.CompletedAt = now;
        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-STOP-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationStopped,
            ProductUnitId = await GetRunUnitIdAsync(run, cancellationToken),
            ManufacturingOrderId = await GetRunOrderIdAsync(run, cancellationToken),
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = $"Simulação '{run.Name}' terminada.",
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["scenarioKey"] = run.ScenarioKey,
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode
            }
        }, cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildDetailByIdAsync(run.Id, cancellationToken);
    }

    public async Task<SimulationRunDetailDto> ResetDemoAsync(int runId, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunAsync(runId, cancellationToken);
        if (!run.Status.Equals(SimulationRunStatuses.Completed, StringComparison.OrdinalIgnoreCase)
            && !run.Status.Equals(SimulationRunStatuses.Stopped, StringComparison.OrdinalIgnoreCase))
        {
            run.Status = SimulationRunStatuses.Stopped;
            run.CompletedAt = DateTime.UtcNow;
            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"SIM-{run.RunCode}-RESET-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                EventType = OperationalEventTypes.SimulationStopped,
                ProductUnitId = await GetRunUnitIdAsync(run, cancellationToken),
                ManufacturingOrderId = await GetRunOrderIdAsync(run, cancellationToken),
                Source = OperationalEventSources.Simulation,
                PerformedByUserId = performedByUserId,
                OccurredAt = DateTime.UtcNow,
                Notes = $"Reset-demo solicitado para '{run.Name}'. O estado relacional foi preservado.",
                IsDemo = true,
                Metadata = new Dictionary<string, object?>
                {
                    ["scenarioKey"] = run.ScenarioKey,
                    ["runId"] = run.Id,
                    ["runCode"] = run.RunCode,
                    ["resetRequested"] = true
                }
            }, cancellationToken: cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        return await BuildDetailByIdAsync(run.Id, cancellationToken);
    }

    private static SimulationScenarioDto ToDto(SimulationScenarioDefinition definition)
    {
        return new SimulationScenarioDto
        {
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            StepCount = definition.StepCount,
            Entities = definition.Entities,
            Highlights = definition.Highlights,
            Outcome = definition.Outcome,
            DefaultUnitCode = definition.DefaultUnitCode,
            DefaultOrderNumber = definition.DefaultOrderNumber
        };
    }

    private async Task<SimulationRunDetailDto> BuildDetailByIdAsync(int runId, CancellationToken cancellationToken)
    {
        var run = await _db.SimulationRuns.AsNoTracking().FirstAsync(x => x.Id == runId, cancellationToken);
        return await BuildDetailAsync(run, cancellationToken);
    }

    private async Task<SimulationRunDetailDto> BuildDetailAsync(SimulationRun run, CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryAsync(run, cancellationToken);
        var steps = await _db.SimulationSteps.AsNoTracking()
            .Where(x => x.SimulationRunId == run.Id)
            .OrderBy(x => x.StepNumber)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return new SimulationRunDetailDto
        {
            Id = summary.Id,
            RunCode = summary.RunCode,
            Name = summary.Name,
            ScenarioKey = summary.ScenarioKey,
            ScenarioName = summary.ScenarioName,
            Description = summary.Description,
            Status = summary.Status,
            ExecutionMode = summary.ExecutionMode,
            CurrentStep = summary.CurrentStep,
            StepCount = summary.StepCount,
            ProgressPercent = summary.ProgressPercent,
            StartedAt = summary.StartedAt,
            PausedAt = summary.PausedAt,
            CompletedAt = summary.CompletedAt,
            LastExecutedAt = summary.LastExecutedAt,
            LastStepType = summary.LastStepType,
            LastStepDescription = summary.LastStepDescription,
            LastResult = summary.LastResult,
            ProductUnitId = summary.ProductUnitId,
            UnitCode = summary.UnitCode,
            ManufacturingOrderId = summary.ManufacturingOrderId,
            OrderNumber = summary.OrderNumber,
            CurrentLine = summary.CurrentLine,
            CurrentSection = summary.CurrentSection,
            SupportCode = summary.SupportCode,
            QualityStatus = summary.QualityStatus,
            QualityDisposition = summary.QualityDisposition,
            RecoveryStatus = summary.RecoveryStatus,
            IsReconditioned = summary.IsReconditioned,
            Notes = run.Notes,
            Steps = steps.Select(step => new SimulationStepDto
            {
                Id = step.Id,
                StepNumber = step.StepNumber,
                StepType = step.StepType,
                Description = step.Description,
                ExecutedAt = step.ExecutedAt,
                ProductUnitId = step.ProductUnitId,
                ManufacturingOrderId = step.ManufacturingOrderId,
                FromLineId = step.FromLineId,
                ToLineId = step.ToLineId,
                FromSectionId = step.FromSectionId,
                ToSectionId = step.ToSectionId,
                Result = step.Result,
                OperationalEventId = step.OperationalEventId
            }).ToList()
        };
    }

    private async Task<SimulationRunSummaryDto> BuildSummaryAsync(SimulationRun run, CancellationToken cancellationToken)
    {
        var scenario = ResolveScenario(run.ScenarioKey);
        var unit = await _db.ProductUnits.AsNoTracking().FirstOrDefaultAsync(x => x.UnitCode == scenario.DefaultUnitCode, cancellationToken);
        var order = await _db.ManufacturingOrders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderNumber == scenario.DefaultOrderNumber, cancellationToken);
        var section = unit is not null && unit.CurrentSectionId.HasValue
            ? await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.CurrentSectionId.Value, cancellationToken)
            : null;
        var line = section is not null && section.LineId.HasValue
            ? await _db.ProductionLines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == section.LineId.Value, cancellationToken)
            : null;
        var support = unit is not null && unit.CurrentSupportId.HasValue
            ? await _db.Supports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.CurrentSupportId.Value, cancellationToken)
            : null;

        var lastStep = await _db.SimulationSteps.AsNoTracking()
            .Where(x => x.SimulationRunId == run.Id)
            .OrderByDescending(x => x.StepNumber)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new SimulationRunSummaryDto
        {
            Id = run.Id,
            RunCode = run.RunCode,
            Name = run.Name,
            ScenarioKey = run.ScenarioKey,
            ScenarioName = scenario.Name,
            Description = scenario.Description,
            Status = run.Status,
            ExecutionMode = run.ExecutionMode,
            CurrentStep = run.CurrentStep,
            StepCount = scenario.StepCount,
            ProgressPercent = scenario.StepCount == 0 ? 0 : Math.Min(100, (int)Math.Round(run.CurrentStep * 100.0 / scenario.StepCount)),
            StartedAt = run.StartedAt,
            PausedAt = run.PausedAt,
            CompletedAt = run.CompletedAt,
            LastExecutedAt = lastStep?.ExecutedAt,
            LastStepType = lastStep?.StepType,
            LastStepDescription = lastStep?.Description,
            LastResult = lastStep?.Result,
            ProductUnitId = unit?.Id,
            UnitCode = unit?.UnitCode,
            ManufacturingOrderId = order?.Id,
            OrderNumber = order?.OrderNumber,
            CurrentLine = line?.Name,
            CurrentSection = section?.Name,
            SupportCode = support?.SupportCode,
            QualityStatus = unit?.QualityStatus,
            QualityDisposition = unit?.QualityDisposition,
            RecoveryStatus = unit?.RecoveryStatus,
            IsReconditioned = unit?.IsReconditioned ?? false
        };
    }

    private async Task<IReadOnlyList<SimulationRunSummaryDto>> BuildSummariesAsync(
        IReadOnlyList<SimulationRun> runs,
        CancellationToken cancellationToken)
    {
        var summaries = new List<SimulationRunSummaryDto>();
        foreach (var run in runs)
        {
            summaries.Add(await BuildSummaryAsync(run, cancellationToken));
        }

        return summaries;
    }

    private async Task<SimulationStepOutcome> ExecuteStepAsync(
        SimulationRun run,
        SimulationScenarioDefinition scenario,
        SimulationAssets assets,
        int stepNumber,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return scenario.Key switch
        {
            "normal-flow" => await ExecuteNormalFlowAsync(run, assets, stepNumber, performedByUserId, now, cancellationToken),
            "minor-recovery" => await ExecuteRecoveryFlowAsync(run, assets, stepNumber, performedByUserId, now, cancellationToken),
            "critical-scrap" => await ExecuteScrapFlowAsync(run, assets, stepNumber, performedByUserId, now, cancellationToken),
            _ => throw new InvalidOperationException($"Cenário de simulação não suportado: {scenario.Key}.")
        };
    }

    private async Task<SimulationStepOutcome> ExecuteNormalFlowAsync(
        SimulationRun run,
        SimulationAssets assets,
        int stepNumber,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return stepNumber switch
        {
            1 => await TransferAsync(run, assets, assets.SupportAssignment, "Atribuição de suporte e entrada em linha", "ASSIGN", performedByUserId, now, cancellationToken),
            2 => await TransferAsync(run, assets, assets.Cutting, "Avanço para estampagem e corte", "TRANSFER", performedByUserId, now, cancellationToken),
            3 => await TransferAsync(run, assets, assets.Welding, "Avanço para soldadura", "TRANSFER", performedByUserId, now, cancellationToken),
            4 => await TransferAsync(run, assets, assets.Painting, "Avanço para pintura", "TRANSFER", performedByUserId, now, cancellationToken),
            5 => await QualityAsync(run, assets, "PASS", "Aprovado em controlo de qualidade", "PASS", performedByUserId, now, cancellationToken),
            6 => await RackAsync(run, assets, "Transferência para rack e fecho do fluxo normal", "RACK", performedByUserId, now, cancellationToken),
            _ => throw new InvalidOperationException("Passo inválido para o cenário de fluxo normal.")
        };
    }

    private async Task<SimulationStepOutcome> ExecuteRecoveryFlowAsync(
        SimulationRun run,
        SimulationAssets assets,
        int stepNumber,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return stepNumber switch
        {
            1 => await TransferAsync(run, assets, assets.SupportAssignment, "Atribuição de suporte e entrada em linha", "ASSIGN", performedByUserId, now, cancellationToken),
            2 => await TransferAsync(run, assets, assets.Cutting, "Avanço para estampagem e corte", "TRANSFER", performedByUserId, now, cancellationToken),
            3 => await TransferAsync(run, assets, assets.Welding, "Avanço para soldadura", "TRANSFER", performedByUserId, now, cancellationToken),
            4 => await TransferAsync(run, assets, assets.Painting, "Avanço para pintura", "TRANSFER", performedByUserId, now, cancellationToken),
            5 => await QualityAsync(run, assets, "FAIL", "Falha menor: risco superficial na pintura", "FAIL", performedByUserId, now, cancellationToken, "Menor"),
            6 => await TransferAsync(run, assets, assets.Rework, "Transferência para retrabalho e recuperação", "REWORK-TRANSFER", performedByUserId, now, cancellationToken),
            7 => await ReconditionAsync(run, assets, markOnly: true, "Unidade marcada como recuperável e retrabalho iniciado", "RECOVERABLE", performedByUserId, now, cancellationToken),
            8 => await ReconditionAsync(run, assets, markOnly: false, "Retrabalho concluído e unidade recondicionada", "RECONDITIONED", performedByUserId, now, cancellationToken),
            9 => await RackAsync(run, assets, "Transferência para rack após recondicionamento", "RACK", performedByUserId, now, cancellationToken),
            _ => throw new InvalidOperationException("Passo inválido para o cenário de recuperação.")
        };
    }

    private async Task<SimulationStepOutcome> ExecuteScrapFlowAsync(
        SimulationRun run,
        SimulationAssets assets,
        int stepNumber,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return stepNumber switch
        {
            1 => await TransferAsync(run, assets, assets.SupportAssignment, "Atribuição de suporte e entrada em linha", "ASSIGN", performedByUserId, now, cancellationToken),
            2 => await TransferAsync(run, assets, assets.Cutting, "Avanço para estampagem e corte", "TRANSFER", performedByUserId, now, cancellationToken),
            3 => await TransferAsync(run, assets, assets.Welding, "Avanço para soldadura", "TRANSFER", performedByUserId, now, cancellationToken),
            4 => await TransferAsync(run, assets, assets.Painting, "Avanço para pintura", "TRANSFER", performedByUserId, now, cancellationToken),
            5 => await QualityAsync(run, assets, "FAIL", "Falha crítica: deformação estrutural fora de tolerância", "FAIL", performedByUserId, now, cancellationToken, "Crítica"),
            6 => await ScrapAsync(run, assets, "Decisão de sucata após falha crítica", "SCRAP", performedByUserId, now, cancellationToken),
            _ => throw new InvalidOperationException("Passo inválido para o cenário de sucata.")
        };
    }

    private async Task<SimulationStepOutcome> TransferAsync(
        SimulationRun run,
        SimulationAssets assets,
        ProductionLineSection targetSection,
        string description,
        string result,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var fromSection = await LoadCurrentSectionAsync(assets.Unit, cancellationToken);
        var fromLine = fromSection?.LineId.HasValue == true
            ? await _db.ProductionLines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fromSection.LineId.Value, cancellationToken)
            : null;
        var transfer = await _flow.TransferAsync(assets.Unit.Id, new ProductUnitTransferRequest
        {
            ToSectionId = targetSection.Id,
            ToSupportId = assets.Support.Id,
            EventType = targetSection.SectionCode == assets.SupportAssignment.SectionCode ? "SimulationSupportAssignment" : "SimulationTransfer",
            Reason = description,
            Notes = description,
            OperatorUserId = performedByUserId,
            MoveCurrentSupport = true
        }, cancellationToken);

        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-{result}-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationProductUnitAdvanced,
            ProductUnitId = assets.Unit.Id,
            ManufacturingOrderId = assets.Order.Id,
            FromProductionLineId = fromLine?.Id,
            ToProductionLineId = targetSection.LineId,
            FromSectionId = fromSection?.Id,
            ToSectionId = targetSection.Id,
            SupportId = assets.Support.Id,
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = description,
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode,
                ["scenarioKey"] = run.ScenarioKey,
                ["step"] = description
            }
        }, saveChanges: false, cancellationToken);

        if (fromLine?.Id != targetSection.LineId)
        {
            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"SIM-{run.RunCode}-LINE-{now:yyyyMMddHHmmssfff}",
                EventType = OperationalEventTypes.SimulationLineTransferExecuted,
                ProductUnitId = assets.Unit.Id,
                ManufacturingOrderId = assets.Order.Id,
                FromProductionLineId = fromLine?.Id,
                ToProductionLineId = targetSection.LineId,
                FromSectionId = fromSection?.Id,
                ToSectionId = targetSection.Id,
                SupportId = assets.Support.Id,
                Source = OperationalEventSources.Simulation,
                PerformedByUserId = performedByUserId,
                OccurredAt = now.AddMilliseconds(1),
                Notes = description,
                IsDemo = true,
                Metadata = new Dictionary<string, object?>
                {
                    ["runId"] = run.Id,
                    ["runCode"] = run.RunCode,
                    ["scenarioKey"] = run.ScenarioKey
                }
            }, saveChanges: false, cancellationToken);
        }

        return new SimulationStepOutcome(
            "Transfer",
            description,
            result,
            assets.Unit.Id,
            assets.Order.Id,
            fromLine?.Id,
            targetSection.LineId,
            fromSection?.Id,
            targetSection.Id);
    }

    private async Task<SimulationStepOutcome> QualityAsync(
        SimulationRun run,
        SimulationAssets assets,
        string qualityResult,
        string description,
        string result,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken,
        string? severity = null)
    {
        var transfer = await TransferAsync(
            run,
            assets,
            assets.Quality,
            "Avanço para controlo de qualidade",
            "QUALITY-TRANSFER",
            performedByUserId,
            now,
            cancellationToken);

        var checkpoint = assets.QualityCheckpoint;
        var quality = await EnsureQualityAsync(assets.Unit, checkpoint, qualityResult, now, description);

        Nonconformity? nonconformity = null;
        if (qualityResult.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            nonconformity = await EnsureNonconformityAsync(
                assets.Unit,
                quality,
                severity ?? "Menor",
                qualityResult.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? "Blocked" : "Open",
                description,
                now);

            assets.Unit.Status = "Blocked";
            assets.Unit.QualityStatus = "FAIL";
            assets.Unit.QualityDisposition = severity is not null && severity.Equals("Crítica", StringComparison.OrdinalIgnoreCase) ? "Blocked" : "Recoverable";
            assets.Unit.RecoveryStatus = severity is not null && severity.Equals("Crítica", StringComparison.OrdinalIgnoreCase) ? "Blocked" : "Candidate";
            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"SIM-{run.RunCode}-QUALITY-FAIL-{now:yyyyMMddHHmmssfff}",
                EventType = OperationalEventTypes.SimulationQualityFailureInjected,
                ProductUnitId = assets.Unit.Id,
                ManufacturingOrderId = assets.Order.Id,
                CheckpointId = checkpoint.Id,
                QualityResultId = quality.Id,
                NonconformityId = nonconformity.Id,
                Severity = severity ?? "Menor",
                Source = OperationalEventSources.Simulation,
                PerformedByUserId = performedByUserId,
                OccurredAt = now,
                Notes = description,
                IsDemo = true,
                Metadata = new Dictionary<string, object?>
                {
                    ["runId"] = run.Id,
                    ["runCode"] = run.RunCode,
                    ["scenarioKey"] = run.ScenarioKey,
                    ["qualityResult"] = qualityResult
                }
            }, saveChanges: false, cancellationToken);

            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"SIM-{run.RunCode}-NC-{now:yyyyMMddHHmmssfff}",
                EventType = OperationalEventTypes.NonconformityOpened,
                ProductUnitId = assets.Unit.Id,
                ManufacturingOrderId = assets.Order.Id,
                CheckpointId = checkpoint.Id,
                QualityResultId = quality.Id,
                NonconformityId = nonconformity.Id,
                Severity = severity ?? "Menor",
                Source = OperationalEventSources.Simulation,
                PerformedByUserId = performedByUserId,
                OccurredAt = now.AddMilliseconds(1),
                Notes = description,
                IsDemo = true,
                Metadata = new Dictionary<string, object?>
                {
                    ["runId"] = run.Id,
                    ["runCode"] = run.RunCode,
                    ["scenarioKey"] = run.ScenarioKey
                }
            }, saveChanges: false, cancellationToken);
        }
        else
        {
            assets.Unit.QualityStatus = "PASS";
            assets.Unit.QualityDisposition = "Normal";
            assets.Unit.RecoveryStatus = "None";
            await _events.RecordAsync(new OperationalEventCreateRequest
            {
                EventCode = $"SIM-{run.RunCode}-QUALITY-PASS-{now:yyyyMMddHHmmssfff}",
                EventType = OperationalEventTypes.QualityRecorded,
                ProductUnitId = assets.Unit.Id,
                ManufacturingOrderId = assets.Order.Id,
                CheckpointId = checkpoint.Id,
                QualityResultId = quality.Id,
                ReasonCode = qualityResult,
                Source = OperationalEventSources.Simulation,
                PerformedByUserId = performedByUserId,
                OccurredAt = now,
                Notes = description,
                IsDemo = true,
                Metadata = new Dictionary<string, object?>
                {
                    ["runId"] = run.Id,
                    ["runCode"] = run.RunCode,
                    ["scenarioKey"] = run.ScenarioKey,
                    ["qualityResult"] = qualityResult
                }
            }, saveChanges: false, cancellationToken);
        }

        return new SimulationStepOutcome(
            "Quality",
            description,
            result,
            assets.Unit.Id,
            assets.Order.Id,
            transfer.FromLineId,
            transfer.ToLineId,
            transfer.FromSectionId,
            transfer.ToSectionId);
    }

    private async Task<SimulationStepOutcome> ReconditionAsync(
        SimulationRun run,
        SimulationAssets assets,
        bool markOnly,
        string description,
        string result,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var access = BuildReconditioningAccess(performedByUserId);
        var nonconformityId = await GetLatestNonconformityIdAsync(assets.Unit.Id, cancellationToken)
            ?? throw new InvalidOperationException("A unidade não tem uma não conformidade recuperável para recondicionamento.");
        if (markOnly)
        {
            await _reconditioning.MarkRecoverableAsync(
                assets.Unit.Id,
                new ReconditioningActionRequest(
                    nonconformityId,
                    null,
                    null,
                    "Falha menor tratada em retrabalho controlado.",
                    description,
                    false),
                access,
                cancellationToken);
        }
        else
        {
            await _reconditioning.CompleteAsync(
                assets.Unit.Id,
                new ReconditioningActionRequest(
                    nonconformityId,
                    null,
                    null,
                    "Validação funcional final concluída após retrabalho.",
                    description,
                    true),
                access,
                cancellationToken);
        }

        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-RECOND-{(markOnly ? "START" : "DONE")}-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationReconditioningPathExecuted,
            ProductUnitId = assets.Unit.Id,
            ManufacturingOrderId = assets.Order.Id,
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = description,
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode,
                ["scenarioKey"] = run.ScenarioKey,
                ["markOnly"] = markOnly
            }
        }, saveChanges: false, cancellationToken);

        return new SimulationStepOutcome(
            "Reconditioning",
            description,
            result,
            assets.Unit.Id,
            assets.Order.Id,
            null,
            null,
            assets.Rework.Id,
            assets.Rework.Id);
    }

    private async Task<SimulationStepOutcome> ScrapAsync(
        SimulationRun run,
        SimulationAssets assets,
        string description,
        string result,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var nonconformityId = await GetLatestNonconformityIdAsync(assets.Unit.Id, cancellationToken);
        if (!nonconformityId.HasValue)
        {
            throw new InvalidOperationException("A unidade não tem uma não conformidade para registar sucata.");
        }
        var nonconformity = await _db.Nonconformities.FirstAsync(x => x.Id == nonconformityId.Value, cancellationToken);
        var scrap = await EnsureScrapAsync(assets.Unit, nonconformity, now, description);
        assets.Unit.Status = "Scrap";
        assets.Unit.QualityStatus = "FAIL";
        assets.Unit.QualityDisposition = "Scrap";
        assets.Unit.RecoveryStatus = "Rejected";

        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-SCRAP-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationScrapPathExecuted,
            ProductUnitId = assets.Unit.Id,
            ManufacturingOrderId = assets.Order.Id,
            NonconformityId = nonconformity.Id,
            ScrapRecordId = scrap.Id,
            Severity = nonconformity.Severity,
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = description,
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode,
                ["scenarioKey"] = run.ScenarioKey
            }
        }, saveChanges: false, cancellationToken);

        return new SimulationStepOutcome(
            "Scrap",
            description,
            result,
            assets.Unit.Id,
            assets.Order.Id,
            null,
            null,
            assets.Quality.Id,
            assets.Quality.Id);
    }

    private async Task<SimulationStepOutcome> RackAsync(
        SimulationRun run,
        SimulationAssets assets,
        string description,
        string result,
        string? performedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _demoEvents.InjectManualEventAsync(new ManualEventRequest
        {
            EventType = "TransferSupportToRack",
            SupportCode = assets.Support.SupportCode,
            SectionCode = assets.Rack.RackCode,
            Notes = description
        });

        await _events.RecordAsync(new OperationalEventCreateRequest
        {
            EventCode = $"SIM-{run.RunCode}-RACK-{now:yyyyMMddHHmmssfff}",
            EventType = OperationalEventTypes.SimulationProductUnitAdvanced,
            ProductUnitId = assets.Unit.Id,
            ManufacturingOrderId = assets.Order.Id,
            ToSectionId = assets.Rack.SectionId,
            RackId = assets.Rack.Id,
            SupportId = assets.Support.Id,
            Source = OperationalEventSources.Simulation,
            PerformedByUserId = performedByUserId,
            OccurredAt = now,
            Notes = description,
            IsDemo = true,
            Metadata = new Dictionary<string, object?>
            {
                ["runId"] = run.Id,
                ["runCode"] = run.RunCode,
                ["scenarioKey"] = run.ScenarioKey
            }
        }, saveChanges: false, cancellationToken);

        return new SimulationStepOutcome(
            "Rack",
            description,
            result,
            assets.Unit.Id,
            assets.Order.Id,
            null,
            null,
            assets.RackSection.Id,
            assets.RackSection.Id);
    }

    private async Task<QualityResult> EnsureQualityAsync(
        ProductUnit unit,
        Checkpoint checkpoint,
        string result,
        DateTime recordedAt,
        string notes)
    {
        var quality = await _db.QualityResults.FirstOrDefaultAsync(x => x.ProductUnitId == unit.Id && x.CheckpointId == checkpoint.Id && x.Result == result);
        if (quality is null)
        {
            quality = new QualityResult
            {
                ProductUnitId = unit.Id,
                CheckpointId = checkpoint.Id,
                Result = result
            };
            _db.QualityResults.Add(quality);
        }

        quality.RecordedAt = recordedAt;
        quality.Notes = notes;
        await _db.SaveChangesAsync();
        return quality;
    }

    private async Task<Nonconformity> EnsureNonconformityAsync(
        ProductUnit unit,
        QualityResult quality,
        string severity,
        string status,
        string description,
        DateTime createdAt)
    {
        var nonconformity = await _db.Nonconformities.FirstOrDefaultAsync(x => x.ProductUnitId == unit.Id && x.QualityResultId == quality.Id);
        if (nonconformity is null)
        {
            nonconformity = new Nonconformity
            {
                ProductUnitId = unit.Id,
                QualityResultId = quality.Id
            };
            _db.Nonconformities.Add(nonconformity);
        }

        nonconformity.Severity = severity;
        nonconformity.Status = status;
        nonconformity.Description = description;
        nonconformity.CreatedAt = createdAt;
        await _db.SaveChangesAsync();
        return nonconformity;
    }

    private async Task<ScrapRecord> EnsureScrapAsync(
        ProductUnit unit,
        Nonconformity nonconformity,
        DateTime scrappedAt,
        string reason)
    {
        var scrap = await _db.ScrapRecords.FirstOrDefaultAsync(x => x.ProductUnitId == unit.Id);
        if (scrap is null)
        {
            scrap = new ScrapRecord { ProductUnitId = unit.Id };
            _db.ScrapRecords.Add(scrap);
        }

        scrap.NonconformityId = nonconformity.Id;
        scrap.ScrappedAt = scrappedAt;
        scrap.Reason = reason;
        await _db.SaveChangesAsync();
        return scrap;
    }

    private async Task<int?> GetLatestNonconformityIdAsync(int productUnitId, CancellationToken cancellationToken)
    {
        return await _db.Nonconformities
            .Where(x => x.ProductUnitId == productUnitId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SimulationRun> LoadRunAsync(int runId, CancellationToken cancellationToken)
    {
        return await _db.SimulationRuns.FirstOrDefaultAsync(x => x.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException($"Simulação com id {runId} não encontrada.");
    }

    private async Task EnsureNoActiveRunForScenarioAsync(string scenarioKey, CancellationToken cancellationToken)
    {
        var active = await _db.SimulationRuns.AnyAsync(x =>
            x.ScenarioKey == scenarioKey &&
            (x.Status == SimulationRunStatuses.Running || x.Status == SimulationRunStatuses.Paused), cancellationToken);
        if (active)
        {
            throw new InvalidOperationException("Já existe uma simulação ativa para este cenário.");
        }
    }

    private async Task<SimulationAssets> LoadScenarioAssetsAsync(SimulationScenarioDefinition scenario, CancellationToken cancellationToken)
    {
        var order = await _db.ManufacturingOrders.FirstOrDefaultAsync(x => x.OrderNumber == scenario.DefaultOrderNumber, cancellationToken)
            ?? throw new InvalidOperationException($"Ordem de fabrico '{scenario.DefaultOrderNumber}' não encontrada.");
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(x => x.UnitCode == scenario.DefaultUnitCode, cancellationToken)
            ?? throw new InvalidOperationException($"Unidade '{scenario.DefaultUnitCode}' não encontrada.");
        var support = await _db.Supports.FirstOrDefaultAsync(x => x.SupportCode == scenario.SupportCode, cancellationToken)
            ?? throw new InvalidOperationException($"Suporte '{scenario.SupportCode}' não encontrado.");
        var rack = await _db.Racks.FirstOrDefaultAsync(x => x.RackCode == scenario.RackCode, cancellationToken)
            ?? throw new InvalidOperationException($"Rack '{scenario.RackCode}' não encontrada.");

        return new SimulationAssets(
            order,
            unit,
            support,
            rack,
            await SectionByCodeAsync("SEC-MP", cancellationToken),
            await SectionByCodeAsync("SEC-ATRIB-SUP", cancellationToken),
            await SectionByCodeAsync("SEC-CORTE-ESTAMP", cancellationToken),
            await SectionByCodeAsync("SEC-SOLD", cancellationToken),
            await SectionByCodeAsync("SEC-PINT-A", cancellationToken),
            await SectionByCodeAsync("SEC-CQ", cancellationToken),
            await SectionByCodeAsync("SEC-RETRAB", cancellationToken),
            await SectionByCodeAsync("SEC-RACK", cancellationToken),
            await CheckpointByCodeAsync("PC-CQ-001", cancellationToken));
    }

    private async Task InitializeScenarioAssetsAsync(SimulationAssets assets, DateTime now, CancellationToken cancellationToken)
    {
        assets.Order.Status = "In Progress";
        assets.Unit.CurrentSectionId = assets.RawMaterials.Id;
        assets.Unit.CurrentSupportId = null;
        assets.Unit.Status = "Planned";
        assets.Unit.QualityStatus = "Pending";
        assets.Unit.IsReconditioned = false;
        assets.Unit.ReconditionedAt = null;
        assets.Unit.ReconditionReason = null;
        assets.Unit.ReconditionedFromNonconformityId = null;
        assets.Unit.ReconditionedByResourceId = null;
        assets.Unit.RecoveryStatus = "None";
        assets.Unit.QualityDisposition = null;
        assets.Unit.CompletedAt = null;

        assets.Support.Status = "Available";
        assets.Support.CurrentSectionId = assets.SupportAssignment.Id;
        assets.Rack.Status = "Available";
        assets.Rack.SectionId = assets.RackSection.Id;

        var openSupportAssignments = await _db.UnitSupportAssignments
            .Where(x => x.ProductUnitId == assets.Unit.Id && x.DateTimeOut == null)
            .ToListAsync(cancellationToken);
        foreach (var assignment in openSupportAssignments)
        {
            assignment.DateTimeOut = now;
        }

        var openRackAssignments = await _db.RackSupportAssignments
            .Where(x => x.RackId == assets.Rack.Id && x.DateTimeOut == null)
            .ToListAsync(cancellationToken);
        foreach (var assignment in openRackAssignments)
        {
            assignment.DateTimeOut = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProductionLineSection> SectionByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.ProductionLineSections.FirstAsync(x => x.SectionCode == code, cancellationToken);
    }

    private async Task<Checkpoint> CheckpointByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.Checkpoints.FirstAsync(x => x.CheckpointCode == code, cancellationToken);
    }

    private async Task<ProductionLineSection?> LoadCurrentSectionAsync(ProductUnit unit, CancellationToken cancellationToken)
    {
        return unit.CurrentSectionId.HasValue
            ? await _db.ProductionLineSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == unit.CurrentSectionId.Value, cancellationToken)
            : null;
    }

    private static ReconditioningAccessContext BuildReconditioningAccess(string? username)
    {
        var user = string.IsNullOrWhiteSpace(username) ? "simulation" : username.Trim();
        return new ReconditioningAccessContext(user, RoleNames.Administrator, null, null, null);
    }

    private static SimulationScenarioDefinition ResolveScenario(string scenarioKey)
    {
        if (ScenarioDefinitions.TryGetValue(scenarioKey, out var scenario))
        {
            return scenario;
        }

        throw new InvalidOperationException($"Cenário de simulação desconhecido: {scenarioKey}.");
    }

    private static string NormalizeMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return SimulationExecutionModes.Manual;
        return mode.Trim().Equals(SimulationExecutionModes.Automatic, StringComparison.OrdinalIgnoreCase)
            ? SimulationExecutionModes.Automatic
            : SimulationExecutionModes.Manual;
    }

    private static bool IsActive(string status)
    {
        return status.Equals(SimulationRunStatuses.Running, StringComparison.OrdinalIgnoreCase)
            || status.Equals(SimulationRunStatuses.Paused, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateRunCodeAsync(string scenarioKey, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var prefix = scenarioKey.Replace("-", string.Empty).Replace("_", string.Empty).ToUpperInvariant();
            if (prefix.Length > 12)
            {
                prefix = prefix[..12];
            }

            var code = $"SIM-{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            if (code.Length > 80)
            {
                code = code[..80];
            }

            if (!await _db.SimulationRuns.AnyAsync(x => x.RunCode == code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código único para a simulação.");
    }

    private async Task<int?> GetRunUnitIdAsync(SimulationRun run, CancellationToken cancellationToken)
    {
        var scenario = ResolveScenario(run.ScenarioKey);
        return await _db.ProductUnits
            .Where(x => x.UnitCode == scenario.DefaultUnitCode)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int?> GetRunOrderIdAsync(SimulationRun run, CancellationToken cancellationToken)
    {
        var scenario = ResolveScenario(run.ScenarioKey);
        return await _db.ManufacturingOrders
            .Where(x => x.OrderNumber == scenario.DefaultOrderNumber)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
