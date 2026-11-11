using EventSignup.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Check constraints
        modelBuilder.Entity<Event>()
            .ToTable(table => table.HasCheckConstraint("CK_Events_Capacity_Positive", "[Capacity] > 0"));
    }
}
