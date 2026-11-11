using EventSignup.Api.Services;
using EventSignup.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EventSignup.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController(EventService eventService) : ControllerBase
{
    /// <summary>Lists events ordered by start time, optionally filtered by date (inclusive, Ljubljana time).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EventSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventSummaryResponse>>> GetEvents(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid date range",
                detail: "Datum 'od' mora biti pred datumom 'do' ali enak njemu.");
        }

        return Ok(await eventService.GetEventsAsync(from, to, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<EventDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailsResponse>> GetEvent(int id, CancellationToken cancellationToken)
    {
        var details = await eventService.GetEventAsync(id, cancellationToken);

        if (details is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Event not found",
                detail: $"Dogodek z ID {id} ne obstaja.");
        }

        return Ok(details);
    }
}
