using DriveTraceCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveTraceCore.Api.Controllers;

[ApiController]
[Route("api/fiware")]
public sealed class FiwareContextController : ControllerBase
{
    private readonly IFiwareContextService _fiware;

    public FiwareContextController(IFiwareContextService fiware)
    {
        _fiware = fiware;
    }

    [HttpGet("context")]
    public async Task<IActionResult> CurrentContext(CancellationToken cancellationToken)
    {
        return Ok(await _fiware.GetContextAsync(cancellationToken));
    }

    [HttpPost("publish-current")]
    public async Task<IActionResult> PublishCurrentContext(CancellationToken cancellationToken)
    {
        return Ok(await _fiware.PublishCurrentContextAsync(cancellationToken));
    }
}
