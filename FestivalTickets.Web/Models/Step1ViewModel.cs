using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

public class Step1ViewModel
{
    public List<Festival> Festivals { get; set; } = new();
    public int? SelectedFestivalId { get; set; }
    public DateOnly FilterFrom { get; set; }
    public DateOnly FilterTo { get; set; }
}
