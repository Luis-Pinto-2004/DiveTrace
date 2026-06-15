using DriveTraceCore.Api.Models;
using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase
{
    private readonly ProductionSimulationService _service;

    public SimulationController(ProductionSimulationService service)
    {
        _service = service;
    }

    [HttpGet("scenarios")]
    public async Task<IActionResult> Scenarios(CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationRead);
        }

        return Ok(await _service.GetScenariosAsync(cancellationToken));
    }

    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationRead);
        }

        return Ok(await _service.GetStateAsync(cancellationToken));
    }

    [HttpGet("runs")]
    public async Task<IActionResult> Runs(CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationRead);
        }

        return Ok(await _service.GetRunsAsync(cancellationToken));
    }

    [HttpGet("runs/{id:int}")]
    public async Task<IActionResult> Run(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationRead))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationRead);
        }

        var run = await _service.GetRunAsync(id, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpPost("runs")]
    public async Task<IActionResult> CreateRun([FromBody] SimulationRunCreateRequest request, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationRun))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationRun);
        }

        try
        {
            return Ok(await _service.CreateRunAsync(request, CurrentUser(), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("runs/{id:int}/tick")]
    public async Task<IActionResult> Tick(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationRun))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationRun);
        }

        return await RunActionAsync(() => _service.TickAsync(id, CurrentUser(), cancellationToken));
    }

    [HttpPost("runs/{id:int}/run-step")]
    public Task<IActionResult> RunStep(int id, CancellationToken cancellationToken)
    {
        return Tick(id, cancellationToken);
    }

    [HttpPost("runs/{id:int}/pause")]
    public async Task<IActionResult> Pause(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationManage);
        }

        return await RunActionAsync(() => _service.PauseAsync(id, CurrentUser(), cancellationToken));
    }

    [HttpPost("runs/{id:int}/resume")]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationManage);
        }

        return await RunActionAsync(() => _service.ResumeAsync(id, CurrentUser(), cancellationToken));
    }

    [HttpPost("runs/{id:int}/stop")]
    public async Task<IActionResult> Stop(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationManage);
        }

        return await RunActionAsync(() => _service.StopAsync(id, CurrentUser(), cancellationToken));
    }

    [HttpPost("runs/{id:int}/reset-demo")]
    public async Task<IActionResult> ResetDemo(int id, CancellationToken cancellationToken)
    {
        if (!Can(PermissionNames.SimulationManage))
        {
            return PermissionCatalogService.Forbidden(PermissionNames.SimulationManage);
        }

        return await RunActionAsync(() => _service.ResetDemoAsync(id, CurrentUser(), cancellationToken));
    }

    private bool Can(string permission)
    {
        return PermissionCatalogService.HasPermission(HttpContext, permission);
    }

    private string? CurrentUser()
    {
        return PermissionCatalogService.ResolveDemoUser(HttpContext).Username;
    }

    private static async Task<IActionResult> RunActionAsync(Func<Task<SimulationRunDetailDto>> action)
    {
        try
        {
            return new OkObjectResult(await action());
        }
        catch (KeyNotFoundException ex)
        {
            return new NotFoundObjectResult(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}
