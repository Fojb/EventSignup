namespace EventSignup.Shared;

public record RegistrationResponse(
    int Id,
    int EventId,
    string FullName,
    string Email,
    DateTimeOffset RegisteredAt);
