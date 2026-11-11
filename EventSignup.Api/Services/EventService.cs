using EventSignup.Data;
using EventSignup.Shared;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Api.Services;

public class EventService(AppDbContext db, TimeProvider timeProvider, EventTimeZone timeZone)
{
    /// <param name="from">First day to include (in the event time zone), or null for no lower bound.</param>
    /// <param name="to">Last day to include (in the event time zone), or null for no upper bound.</param>
    public async Task<IReadOnlyList<EventSummaryResponse>> GetEventsAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var query = db.Events.AsNoTracking();

        if (from is not null)
        {
            var start = timeZone.StartOfDay(from.Value);
            query = query.Where(e => e.StartsAt >= start);
        }

        if (to is not null)
        {
            var endExclusive = timeZone.StartOfDay(to.Value.AddDays(1));
            query = query.Where(e => e.StartsAt < endExclusive);
        }

        return await query
            .OrderBy(e => e.StartsAt)
            .Select(e => new EventSummaryResponse(
                e.Id,
                e.Title,
                e.Location,
                e.StartsAt,
                e.Capacity,
                e.Capacity - e.Registrations.Count,
                e.StartsAt <= now))
            .ToListAsync(cancellationToken);
    }

    public async Task<EventDetailsResponse?> GetEventAsync(int id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return await db.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventDetailsResponse(
                e.Id,
                e.Title,
                e.Description,
                e.Location,
                e.StartsAt,
                e.Capacity,
                e.Registrations.Count,
                e.Capacity - e.Registrations.Count,
                e.StartsAt <= now))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
