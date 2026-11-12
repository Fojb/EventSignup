namespace EventSignup.Shared;

/// <summary>The structured filter the query was turned into; the database search uses only this.</summary>
public record EventSearchFilter(
    DateOnly? From,
    DateOnly? To,
    string? Location,
    IReadOnlyList<string> Keywords);

/// <param name="UsedLlm">False when the LLM was unavailable and a plain keyword search was used instead.</param>
public record EventSearchResponse(
    EventSearchFilter Filter,
    bool UsedLlm,
    IReadOnlyList<EventSummaryResponse> Events);
