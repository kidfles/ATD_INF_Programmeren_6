using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

/// <summary>
/// Een bevestigde boeking geplaatst door een klant voor een specifiek pakket (ticket).
/// </summary>
public sealed class Booking
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Pakket")]
    public int PackageId { get; set; }
    public Package? Package { get; set; }

    /// <summary>Aantal tickets (kopieën van het pakket) geboekt.</summary>
    [Range(1, 1000)]
    [Display(Name = "Aantal tickets")]
    public int Quantity { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Klant")]
    public int CustId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Boekingsdatum")]
    public DateTime BookingDate { get; set; }

    /// <summary>Definitieve totaalprijs na alle kortingen, opgeslagen op het moment van boeken.</summary>
    [Range(0, double.MaxValue)]
    [DataType(DataType.Currency)]
    [Display(Name = "Totaalprijs")]
    public decimal TotalPricePaid { get; set; }

    public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
}
