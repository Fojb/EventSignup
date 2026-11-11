using System.ComponentModel.DataAnnotations;

namespace EventSignup.Shared;

public class EventSearchRequest
{
    public const int QueryMaxLength = 200;

    [Required(ErrorMessage = "Vnesite, kaj iščete.")]
    [MaxLength(QueryMaxLength, ErrorMessage = "Iskalni niz je lahko dolg največ 200 znakov.")]
    public string Query { get; set; } = "";
}
