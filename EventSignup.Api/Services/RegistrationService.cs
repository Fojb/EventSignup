using System.Data;
using EventSignup.Data;
using EventSignup.Data.Entities;
using EventSignup.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Api.Services;

public enum RegistrationError
{
    EventNotFound,
    EventInPast,
    AlreadyRegistered,
    EventFull,
}

public record RegistrationResult(RegistrationResponse? Registration, RegistrationError? Error)
{
    public static RegistrationResult Success(RegistrationResponse registration) => new(registration, null);
    public static RegistrationResult Failure(RegistrationError error) => new(null, error);
}

public class RegistrationService(AppDbContext db, TimeProvider timeProvider)
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<RegistrationResult> RegisterAsync(
        int eventId, CreateRegistrationRequest request, CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        // Serializable: two concurrent requests can't both take the last spot (the loser deadlocks and is retried).
        // A user transaction with EnableRetryOnFailure must run inside the execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var target = await db.Events
                .Where(e => e.Id == eventId)
                .Select(e => new { e.StartsAt, e.Capacity, RegisteredCount = e.Registrations.Count })
                .FirstOrDefaultAsync(ct);

            if (target is null)
            {
                return RegistrationResult.Failure(RegistrationError.EventNotFound);
            }

            var now = timeProvider.GetUtcNow();

            if (target.StartsAt <= now)
            {
                return RegistrationResult.Failure(RegistrationError.EventInPast);
            }

            if (await db.Registrations.AnyAsync(r => r.EventId == eventId && r.Email == email, ct))
            {
                return RegistrationResult.Failure(RegistrationError.AlreadyRegistered);
            }

            if (target.RegisteredCount >= target.Capacity)
            {
                return RegistrationResult.Failure(RegistrationError.EventFull);
            }

            var registration = new Registration
            {
                EventId = eventId,
                FullName = fullName,
                Email = email,
                RegisteredAt = now,
            };
            db.Registrations.Add(registration);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException
                       { Number: UniqueIndexViolation or UniqueConstraintViolation })
            {
                return RegistrationResult.Failure(RegistrationError.AlreadyRegistered);
            }

            await transaction.CommitAsync(ct);

            return RegistrationResult.Success(new RegistrationResponse(
                registration.Id,
                registration.EventId,
                registration.FullName,
                registration.Email,
                registration.RegisteredAt));
        }, cancellationToken);
    }
}
