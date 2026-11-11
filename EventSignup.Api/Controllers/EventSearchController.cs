using EventSignup.Api.Search;
using EventSignup.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventSignup.Api.Controllers;

[ApiController]
[Route("events/search")]
public class EventSearchController(EventSearchService searchService) : ControllerBase
{
    public const string RateLimitPolicy = "search";

    /// <summary>Natural-language search, e.g. "koncerti v Ljubljani naslednji teden".</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType<EventSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<EventSearchResponse>> Search(
        EventSearchRequest request,
        CancellationToken cancellationToken) =>
        Ok(await searchService.SearchAsync(request.Query, cancellationToken));
}
