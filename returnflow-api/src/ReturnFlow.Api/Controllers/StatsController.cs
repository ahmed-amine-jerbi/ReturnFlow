using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReturnFlow.Api.Dtos;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize(Roles = "Manager,Admin")]
public class StatsController(StatsService stats) : ControllerBase
{
    /// <summary>KPIs for one time range, compared with the period before it.</summary>
    /// <param name="range">d1 (last 24 hours), d7 (last 7 days) or lifetime.</param>
    /// <param name="ct">Cancelled when the client disconnects.</param>
    [HttpGet]
    public async Task<ActionResult<StatsResponse>> Get([FromQuery] string range = "d7", CancellationToken ct = default)
        => Ok(await stats.GetAsync(range, ct));
}
