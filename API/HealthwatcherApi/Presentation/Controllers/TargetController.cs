using HealthwatcherApi.Application.Contracts;
using HealthwatcherApi.Application.Contracts.DTOs.Target;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace HealthwatcherApi.Presentation.Controllers;

[ApiController]
[Route("api/targets")]
[Produces("application/json")]
[ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
public class TargetController : ControllerBase
{
    private readonly ITargetService _targetService;

    public TargetController(ITargetService targetService)
    {
        _targetService = targetService;
    }

    [HttpGet("{id:guid}", Name = nameof(GetTargetById))]
    [ProducesResponseType<TargetDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TargetDto>> GetTargetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _targetService.GetTargetById(id, cancellationToken));

    [HttpGet]
    [ProducesResponseType<PagedResult<PreviewTargetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PreviewTargetDto>>> GetTargets(
        [FromQuery] PageRequest page, CancellationToken cancellationToken) =>
        Ok(await _targetService.GetTargets(page, cancellationToken));

    // backs the response-time chart
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<PagedResult<TargetHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TargetHistoryDto>>> GetTargetHistory(
        Guid id,
        [FromQuery] PageRequest page,
        [FromQuery] int? withinHours,
        CancellationToken cancellationToken) =>
        Ok(await _targetService.GetTargetHistory(id, page, withinHours, cancellationToken));

    [HttpGet("{id:guid}/uptime")]
    [ProducesResponseType<TargetUptimeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TargetUptimeDto>> GetTargetUptime(
        Guid id,
        [FromQuery] int windowHours = 24,
        CancellationToken cancellationToken = default) =>
        Ok(await _targetService.GetTargetUptime(id, windowHours, cancellationToken));

    [HttpPost]
    [ProducesResponseType<PreviewTargetDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PreviewTargetDto>> InsertTarget(
        [FromBody] InsertTargetDto insertTargetDto, CancellationToken cancellationToken)
    {
        PreviewTargetDto target = await _targetService.InsertTarget(insertTargetDto, cancellationToken);

        return CreatedAtRoute(nameof(GetTargetById), new { id = target.Id }, target);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RenameTarget(
        Guid id, [FromBody] RenameTargetDto renameTargetDto, CancellationToken cancellationToken)
    {
        await _targetService.RenameTarget(id, renameTargetDto, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteTarget(Guid id, CancellationToken cancellationToken)
    {
        await _targetService.DeleteTarget(id, cancellationToken);

        return NoContent();
    }

}
