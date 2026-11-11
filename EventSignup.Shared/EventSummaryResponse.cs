namespace EventSignup.Shared;

public record EventSummaryResponse(
    int Id,
    string Title,
    string Location,
    DateTimeOffset StartsAt,
    int Capacity,
    int FreeSpots,
    bool IsPast);
