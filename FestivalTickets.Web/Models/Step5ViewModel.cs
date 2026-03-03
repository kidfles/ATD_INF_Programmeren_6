using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

public class Step5ViewModel
{
    public Package? Package { get; set; }
    public int TicketQuantity { get; set; }
    public List<WizardItemEntry> ExtraItems { get; set; } = new();
    public decimal BaseTotal { get; set; }
    public List<DiscountLine> DiscountLines { get; set; } = new();
    public decimal FinalTotal { get; set; }
}
