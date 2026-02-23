using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FestivalTickets.Web.Models;

// This is the form we use to create or edit a festival.
public sealed class FestivalFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Plaats")]
    public string Place { get; set; } = string.Empty;

    [Display(Name = "Beschrijving")]
    [MaxLength(2000)]
    public string? Description { get; set; }

    [Display(Name = "Basisprijs")]
    [DataType(DataType.Currency)]
    [Range(0, 100000, ErrorMessage = "De basisprijs kan niet negatief zijn.")]
    public decimal BasicPrice { get; set; }

    [Display(Name = "Startdatum")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Einddatum")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Logo (PNG)")]
    public IFormFile? LogoFile { get; set; }

    public string? ExistingLogoPath { get; set; }

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
