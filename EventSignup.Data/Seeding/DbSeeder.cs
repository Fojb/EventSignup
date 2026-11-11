using EventSignup.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Data.Seeding;

/// <summary>
/// Inserts sample events into an empty database. Dates are relative to today,
/// so the app always has upcoming events, plus one past and one full event
/// to demonstrate the registration rules.
/// </summary>
public static class DbSeeder
{
    public static void Seed(DbContext context, EventTimeZone timeZone)
    {
        if (context.Set<Event>().Any())
        {
            return;
        }

        context.Set<Event>().AddRange(CreateSampleEvents(DateTimeOffset.UtcNow, timeZone));
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, EventTimeZone timeZone, CancellationToken cancellationToken)
    {
        if (await context.Set<Event>().AnyAsync(cancellationToken))
        {
            return;
        }

        context.Set<Event>().AddRange(CreateSampleEvents(DateTimeOffset.UtcNow, timeZone));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<Event> CreateSampleEvents(DateTimeOffset now, EventTimeZone timeZone)
    {
        var today = timeZone.Today(now);

        DateTimeOffset At(int daysFromToday, int hour, int minute = 0) =>
            timeZone.At(today.AddDays(daysFromToday), new TimeOnly(hour, minute));

        Registration Attendee(string fullName, string email) => new()
        {
            FullName = fullName,
            Email = email,
            RegisteredAt = now.AddDays(-1),
        };

        return
        [
            new Event
            {
                Title = "Jazz večer",
                Description = "Koncert jazz kvarteta s standardi in avtorskimi skladbami.",
                Location = "Kino Šiška, Ljubljana",
                StartsAt = At(3, 20),
                Capacity = 150,
            },
            new Event
            {
                Title = "Simfonični koncert: Beethoven in Brahms",
                Description = "Večer klasične glasbe s simfoničnim orkestrom.",
                Location = "Cankarjev dom, Ljubljana",
                StartsAt = At(8, 19, 30),
                Capacity = 400,
            },
            new Event
            {
                Title = "Akustični koncert ob Dravi",
                Description = "Kantavtorski koncert na prostem ob reki.",
                Location = "Lent, Maribor",
                StartsAt = At(10, 20),
                Capacity = 200,
            },
            new Event
            {
                Title = "Razstava sodobne slovenske umetnosti",
                Description = "Vodeni ogled razstave slik in instalacij mladih avtorjev.",
                Location = "Moderna galerija, Ljubljana",
                StartsAt = At(2, 10),
                Capacity = 80,
            },
            new Event
            {
                Title = "Delavnica fotografije za začetnike",
                Description = "Osnove kompozicije, svetlobe in nastavitev fotoaparata.",
                Location = "Celjski dom, Celje",
                StartsAt = At(5, 17),
                Capacity = 15,
            },
            new Event
            {
                Title = "Delavnica peke kruha",
                Description = "Praktična delavnica peke kruha z drožmi. Skoraj zasedeno.",
                Location = "Stara pekarna, Celje",
                StartsAt = At(4, 16),
                Capacity = 3,
                Registrations =
                [
                    Attendee("Ana Novak", "ana.novak@example.com"),
                    Attendee("Marko Kovač", "marko.kovac@example.com"),
                ],
            },
            new Event
            {
                Title = "Degustacija istrskih vin",
                Description = "Vodena degustacija z lokalnimi vinarji. Razprodano.",
                Location = "Vinska klet, Koper",
                StartsAt = At(7, 19),
                Capacity = 2,
                Registrations =
                [
                    Attendee("Petra Horvat", "petra.horvat@example.com"),
                    Attendee("Luka Krajnc", "luka.krajnc@example.com"),
                ],
            },
            new Event
            {
                Title = "Konferenca o umetni inteligenci",
                Description = "Predavanja in okrogle mize o uporabi umetne inteligence v podjetjih.",
                Location = "Gospodarsko razstavišče, Ljubljana",
                StartsAt = At(20, 9),
                Capacity = 500,
            },
            new Event
            {
                Title = "Tek ob morju (5 km)",
                Description = "Rekreativni tek po obalni poti za vse starosti.",
                Location = "Obala, Koper",
                StartsAt = At(14, 10),
                Capacity = 300,
            },
            new Event
            {
                Title = "Rekreativni košarkarski turnir",
                Description = "Turnir ekip po tri igralce.",
                Location = "Dvorana Tabor, Maribor",
                StartsAt = At(16, 9),
                Capacity = 64,
            },
            new Event
            {
                Title = "Martinovanje",
                Description = "Tradicionalno praznovanje z mladim vinom in glasbo. Dogodek je že minil.",
                Location = "Glavni trg, Maribor",
                StartsAt = At(-5, 18),
                Capacity = 100,
                Registrations =
                [
                    Attendee("Nina Zupan", "nina.zupan@example.com"),
                ],
            },
        ];
    }
}
