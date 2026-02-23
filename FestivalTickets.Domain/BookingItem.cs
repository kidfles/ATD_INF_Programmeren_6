using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

/// <summary>
/// Koppeltabel: extra items gekozen door de klant tijdens de boekingswizard (Stap 3).
/// In tegenstelling tot PackageItems, zijn deze vrij gekozen — geen ItemType limiet, willekeurig aantal.
/// </summary>
public sealed class BookingItem
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Aantal")]
    public int Quantity { get; set; }
}
