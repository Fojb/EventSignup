using EventSignup.Data;
using EventSignup.Data.Entities;
using EventSignup.Shared;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Api.Services;

public class EventService(AppDbContext db, TimeProvider timeProvider, EventTimeZone timeZone)
{
    /// <param name="from">First day to include (in the event time zone), or null for no lower bound.</param>
    /// <param name="to">Last day to include (in the event time zone), or null for no upper bound.</param>
    public Task<IReadOnlyList<EventSummaryResponse>> GetEventsAsync(
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken) =>
        ToSummariesAsync(FilterByDate(db.Events.AsNoTracking(), from, to), cancellationToken);

    /// <summary>Location must match; at least one keyword must appear in the title, description or location.</summary>
    public Task<IReadOnlyList<EventSummaryResponse>> SearchAsync(
        EventSearchFilter filter, CancellationToken cancellationToken)
    {
        var query = FilterByDate(db.Events.AsNoTracking(), filter.From, filter.To);

        if (!string.IsNullOrEmpty(filter.Location))
        {
            query = query.Where(e => e.Location.Contains(filter.Location));
        }

        if (filter.Keywords.Count > 0)
        {
            var keywords = filter.Keywords.ToList();
            query = query.Where(e => keywords.Any(k =>
                e.Title.Contains(k) ||
                (e.Description != null && e.Description.Contains(k)) ||
                e.Location.Contains(k)));
        }

        return ToSummariesAsync(query, cancellationToken);
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

    private IQueryable<Event> FilterByDate(IQueryable<Event> query, DateOnly? from, DateOnly? to)
    {
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

        return query;
    }

    private async Task<IReadOnlyList<EventSummaryResponse>> ToSummariesAsync(
        IQueryable<Event> query, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

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
}
