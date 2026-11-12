using System.ComponentModel.DataAnnotations;

namespace EventSignup.Shared;

/// <summary>
/// Validated by the API ([ApiController]) and by the Blazor form (DataAnnotationsValidator).
/// Max lengths must match the Registration entity columns.
/// </summary>
public class CreateRegistrationRequest
{
    public const int FullNameMaxLength = 200;
    public const int EmailMaxLength = 256;

    [Required(ErrorMessage = "Vnesite ime in priimek.")]
    [MaxLength(FullNameMaxLength, ErrorMessage = "Ime in priimek je lahko dolgo največ 200 znakov.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vnesite e-poštni naslov.")]
    [EmailAddress(ErrorMessage = "E-poštni naslov ni veljaven.")]
    [MaxLength(EmailMaxLength, ErrorMessage = "E-poštni naslov je lahko dolg največ 256 znakov.")]
    public string Email { get; set; } = "";
}
