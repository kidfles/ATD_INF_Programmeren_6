using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

public class Step3ViewModel
{
    public List<Item> AllItems { get; set; } = new();
    public List<WizardItemEntry> ExtraItems { get; set; } = new();
    public List<int>? SelectedItemIds { get; set; }
    public List<int>? Quantities { get; set; }
}
