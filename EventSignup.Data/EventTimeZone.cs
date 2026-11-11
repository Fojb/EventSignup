namespace EventSignup.Data;

/// <summary>
/// The time zone in which event dates are interpreted (e.g. the date filter and "today").
/// The database stores exact instants (datetimeoffset); this converts calendar dates to and from them.
/// </summary>
public class EventTimeZone(TimeZoneInfo timeZone)
{
    public TimeZoneInfo TimeZone { get; } = timeZone;

    /// <param name="id">IANA time zone id, e.g. "Europe/Ljubljana".</param>
    public static EventTimeZone FromId(string id) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone)
            ? new EventTimeZone(timeZone)
            : throw new InvalidOperationException($"Time zone '{id}' was not found on this system.");

    public DateOnly Today(DateTimeOffset now) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZone).DateTime);

    public DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        return new DateTimeOffset(local, TimeZone.GetUtcOffset(local));
    }

    public DateTimeOffset StartOfDay(DateOnly date) => At(date, TimeOnly.MinValue);
}
