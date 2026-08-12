using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>Unauthenticated operational endpoints.</summary>
[ApiController]
[AllowAnonymous]
public sealed class HealthController(IStatusService statusService) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy" });

    /// <summary>Service status including per-dataset data freshness from the ingestion log.</summary>
    [HttpGet("/status")]
    public async Task<IActionResult> Status(
        [FromQuery] string? report,
        [FromQuery] string? dataSet,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (string.Equals(report, "coverage", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(await statusService.GetCoreCoverageAsync(cancellationToken));
        }

        if (string.Equals(report, "history", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(await statusService.GetHistoryAsync(dataSet, page ?? 1, pageSize ?? 50, cancellationToken));
        }

        return Ok(await statusService.GetStatusAsync(cancellationToken));
    }

    /// <summary>Detailed activity history for ingestion/status transparency.</summary>
    [HttpGet("/status/history")]
    public async Task<IActionResult> StatusHistory(
        [FromQuery] string? dataSet,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var history = await statusService.GetHistoryAsync(
            dataSet,
            page ?? 1,
            pageSize ?? 50,
            cancellationToken);
        return Ok(history);
    }

    [HttpGet("/status/coverage")]
    public async Task<IActionResult> StatusCoverage(CancellationToken cancellationToken)
        => Ok(await statusService.GetCoreCoverageAsync(cancellationToken));
}
