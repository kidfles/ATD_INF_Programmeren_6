using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

// Represents a festival in the system.
public sealed class Festival : IValidatableObject
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Plaats")]
    public string Place { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Logo")]
    public string? Logo { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Beschrijving")]
    public string? Description { get; set; }

    [Display(Name = "Basisprijs")]
    [DataType(DataType.Currency)]
    [Range(0, 100000, ErrorMessage = "De basisprijs kan niet negatief zijn.")]
    public decimal BasicPrice { get; set; }

    [Display(Name = "Startdatum")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }
    [Display(Name = "Einddatum")]
    [DataType(DataType.Date)]
    public DateOnly EndDate   { get; set; }

    public ICollection<Package> Packages { get; set; } = new List<Package>();

    // Validation to make sure the end date isn't before the start date. That would be weird.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "De einddatum moet op of na de startdatum liggen.",
                new[] { nameof(EndDate), nameof(StartDate) });
        }
    }
}


