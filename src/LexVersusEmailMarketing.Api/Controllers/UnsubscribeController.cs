using LexVersusEmailMarketing.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexVersusEmailMarketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class UnsubscribeController(IUnsubscribeService unsubscribeService) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> GetInfo(string token, CancellationToken ct)
    {
        var info = await unsubscribeService.GetByTokenAsync(token, ct);
        return info is null ? NotFound("Invalid unsubscribe link.") : Ok(info);
    }

    [HttpPost("{token}")]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken ct)
    {
        var result = await unsubscribeService.UnsubscribeAsync(token, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
