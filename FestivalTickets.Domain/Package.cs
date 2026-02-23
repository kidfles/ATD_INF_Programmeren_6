using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

// A bundle of fun (and items) for a specific festival.
public sealed class Package
{
     public int Id { get; set; }

    [Display(Name = "Festival")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecteer een festival.")]
    public int FestivalId { get; set; }

    public Festival? Festival { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    public ICollection<PackageItem> PackageItems { get; set; }
        = new List<PackageItem>();
}


