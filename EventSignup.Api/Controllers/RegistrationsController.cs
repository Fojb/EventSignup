using EventSignup.Api.Services;
using EventSignup.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EventSignup.Api.Controllers;

[ApiController]
[Route("events/{eventId:int}/registrations")]
public class RegistrationsController(RegistrationService registrationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RegistrationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RegistrationResponse>> Register(
        int eventId,
        CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.RegisterAsync(eventId, request, cancellationToken);

        return result.Error switch
        {
            null => StatusCode(StatusCodes.Status201Created, result.Registration),
            RegistrationError.EventNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Event not found",
                detail: $"Dogodek z ID {eventId} ne obstaja."),
            RegistrationError.EventInPast => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Event already started",
                detail: "Na dogodek, ki je že minil, se ni mogoče prijaviti."),
            RegistrationError.AlreadyRegistered => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Already registered",
                detail: "S tem e-poštnim naslovom ste na dogodek že prijavljeni."),
            RegistrationError.EventFull => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Event is full",
                detail: "Dogodek je zaseden, prostih mest ni več."),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Error), result.Error, null),
        };
    }
}
