using System.Globalization;
using System.Text.RegularExpressions;
using EventSignup.Api.Services;
using EventSignup.Data;
using EventSignup.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace EventSignup.Api.Search;

/// <summary>
/// The LLM only turns the query into a filter; the database does the search.
/// Without an API key, or when the LLM fails, a plain keyword search is used instead.
/// </summary>
public partial class EventSearchService(
    LlmQueryParser parser,
    EventService eventService,
    IMemoryCache cache,
    TimeProvider timeProvider,
    EventTimeZone timeZone)
{
    private const int MaxKeywords = 5;
    private const int MinKeywordLength = 3;
    private const int MaxKeywordLength = 40;
    private const int MaxLocationLength = 100;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<EventSearchResponse> SearchAsync(string query, CancellationToken cancellationToken)
    {
        query = Whitespace().Replace(query.Trim(), " ");
        var today = timeZone.Today(timeProvider.GetUtcNow());

        var filter = await ParseWithLlmAsync(query, today, cancellationToken);
        var usedLlm = filter is not null;
        filter ??= KeywordFilter(query);

        var events = await eventService.SearchAsync(filter, cancellationToken);
        return new EventSearchResponse(filter, usedLlm, events);
    }

    private async Task<EventSearchFilter?> ParseWithLlmAsync(string query, DateOnly today, CancellationToken cancellationToken)
    {
        if (!parser.IsConfigured)
        {
            return null;
        }

        // Relative dates ("jutri") depend on today, so it is part of the key.
        var cacheKey = $"search:{today:yyyy-MM-dd}:{query.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out EventSearchFilter? cached))
        {
            return cached;
        }

        var raw = await parser.ParseAsync(query, today, cancellationToken);
        if (raw is null)
        {
            return null;
        }

        var filter = Sanitize(raw, today);
        cache.Set(cacheKey, filter, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration, Size = 1 });
        return filter;
    }

    /// <summary>Keeps only well-formed, bounded values; anything else is dropped.</summary>
    private static EventSearchFilter Sanitize(LlmFilter raw, DateOnly today)
    {
        var from = ParseDate(raw.From, today);
        var to = ParseDate(raw.To, today);
        if (from > to)
        {
            (from, to) = (to, from);
        }

        var keywords = raw.Keywords
            .Select(k => Clean(k, MaxKeywordLength))
            .OfType<string>()
            .Where(k => k.Length >= MinKeywordLength)
            .Select(k => k.ToLowerInvariant())
            .Distinct()
            .Take(MaxKeywords)
            .ToList();

        return new EventSearchFilter(from, to, Clean(raw.Location, MaxLocationLength), keywords);
    }

    private static DateOnly? ParseDate(string? value, DateOnly today) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        && date >= today.AddYears(-1) && date <= today.AddYears(2)
            ? date
            : null;

    private static string? Clean(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength ? null : trimmed;
    }

    /// <summary>Fallback: the query's own words. Dropping the last letter of longer words lets "koncerti" match "koncert".</summary>
    private static EventSearchFilter KeywordFilter(string query)
    {
        var keywords = NonWordCharacters().Split(query.ToLowerInvariant())
            .Where(w => w.Length >= MinKeywordLength)
            .Select(w => w.Length > 4 ? w[..^1] : w)
            .Distinct()
            .Take(MaxKeywords)
            .ToList();

        return new EventSearchFilter(null, null, null, keywords);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonWordCharacters();
}
