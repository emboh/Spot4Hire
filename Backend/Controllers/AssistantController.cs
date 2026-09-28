using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Assistant;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Controllers;

[ApiController]
[Route("api/assistant")]
public class AssistantController(IAssistantService assistant) : ControllerBase
{
    // POST api/assistant/ask
    [HttpPost("ask")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Assistant)]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AskResponse>> Ask(AskRequest request, CancellationToken ct)
        => Ok(await assistant.AskAsync(request, ct));
}
