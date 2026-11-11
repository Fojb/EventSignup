using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using EventSignup.Shared;

namespace EventSignup.Client.Services;

public class EventsApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<EventSummaryResponse>> GetEventsAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from is not null) query.Add($"from={FormatDate(from.Value)}");
        if (to is not null) query.Add($"to={FormatDate(to.Value)}");

        var url = query.Count == 0 ? "events" : $"events?{string.Join('&', query)}";

        using var response = await http.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<EventSummaryResponse>>(cancellationToken) ?? [];
    }

    /// <returns>The event, or null if it does not exist.</returns>
    public async Task<EventDetailsResponse?> GetEventAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"events/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<EventDetailsResponse>(cancellationToken);
    }

    public async Task<RegistrationResponse> RegisterAsync(
        int eventId, CreateRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync($"events/{eventId}/registrations", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return (await response.Content.ReadFromJsonAsync<RegistrationResponse>(cancellationToken))!;
    }

    public async Task<EventSearchResponse> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "events/search", new EventSearchRequest { Query = query }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return (await response.Content.ReadFromJsonAsync<EventSearchResponse>(cancellationToken))!;
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // Turns an API error (RFC 7807 ProblemDetails) into an exception with a user-readable message.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ApiProblem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken);
        }
        catch (Exception)
        {
            // Body was not ProblemDetails JSON; fall back to the status code below.
        }

        // Validation errors (400) carry their messages in "errors" instead of "detail".
        var validationMessages = problem?.Errors?.SelectMany(e => e.Value).ToList();
        var message = validationMessages is { Count: > 0 }
            ? string.Join(" ", validationMessages)
            : problem?.Detail ?? problem?.Title ?? $"Zahteva ni uspela ({(int)response.StatusCode}).";

        throw new ApiException(message);
    }

    private record ApiProblem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}

public class ApiException(string message) : Exception(message);
