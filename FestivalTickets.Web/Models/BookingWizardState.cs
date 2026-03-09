using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

/// <summary>
/// Stored in HttpContext.Session as JSON. Persists across wizard steps.
/// Reset on Cancel or after successful confirmation.
/// </summary>
public class BookingWizardState
{
    // Step 1
    public int? SelectedFestivalId { get; set; }
    public DateOnly FilterFrom { get; set; }
    public DateOnly FilterTo { get; set; }

    // Step 2
    public int? SelectedPackageId { get; set; }
    public int TicketQuantity { get; set; } = 1;

    // Step 3
    public List<WizardItemEntry> ExtraItems { get; set; } = new();

    // Step 5
    public decimal BaseTotal { get; set; }
    public decimal FinalTotal { get; set; }
    public List<DiscountLine> Discounts { get; set; } = new();
}

public class WizardItemEntry
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class DiscountLine
{
    public string Name { get; set; } = string.Empty;
    public decimal Saving { get; set; }
}
