using System;
using System.Collections.Generic;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Bevat alle informatie die nodig is voor kortingsstrategieën om beslissingen te nemen.
/// Doorgegeven aan elke IDiscountStrategy.ApplyDiscount() aanroep.
/// </summary>
public sealed class DiscountContext
{
    /// <summary>Basisprijs voor kortingen: Festival.BasicPrice × ticket aantal + alle item prijzen.</summary>
    public decimal BaseTotal { get; set; }

    /// <summary>Lopend totaal: begint als BaseTotal, gewijzigd door elke strategie in volgorde.</summary>
    public decimal CurrentTotal { get; set; }

    /// <summary>Hoeveel tickets (pakket kopieën) geboekt worden.</summary>
    public int TicketQuantity { get; set; }

    /// <summary>De datum waarop de boeking wordt geplaatst.</summary>
    public DateTime BookingDate { get; set; }

    /// <summary>De startdatum van het festival (voor vroegboekkorting berekening).</summary>
    public DateOnly FestivalStartDate { get; set; }

    /// <summary>De extra items toegevoegd door de klant in Stap 3 (ItemId → aantal).</summary>
    public IList<(int ItemId, ItemType ItemType, decimal UnitPrice, int Quantity)> ExtraItems { get; set; }
        = new List<(int, ItemType, decimal, int)>();

    /// <summary>True als de klant de Loyalty Card claim heeft.</summary>
    public bool HasLoyaltyCard { get; set; }
}
