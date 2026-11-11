namespace EventSignup.Shared;

public record EventDetailsResponse(
    int Id,
    string Title,
    string? Description,
    string Location,
    DateTimeOffset StartsAt,
    int Capacity,
    int RegisteredCount,
    int FreeSpots,
    bool IsPast);
