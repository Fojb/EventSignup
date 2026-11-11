using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventSignup.Data.Entities;

[Index(nameof(StartsAt))]
public class Event
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int LocationMaxLength = 200;

    public int Id { get; set; }

    [MaxLength(TitleMaxLength)]
    public required string Title { get; set; }

    [MaxLength(DescriptionMaxLength)]
    public string? Description { get; set; }

    [MaxLength(LocationMaxLength)]
    public required string Location { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public int Capacity { get; set; }

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
