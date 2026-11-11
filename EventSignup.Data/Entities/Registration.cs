using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Data.Entities;


[Index(nameof(EventId), nameof(Email), IsUnique = true)]
public class Registration
{
    public const int FullNameMaxLength = 200;
    public const int EmailMaxLength = 256;

    public int Id { get; set; }

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    [MaxLength(FullNameMaxLength)]
    public required string FullName { get; set; }

    [MaxLength(EmailMaxLength)]
    public required string Email { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }
}
