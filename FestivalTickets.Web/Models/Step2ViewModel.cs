using FestivalTickets.Domain;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public class Step2ViewModel
{
    public Festival? Festival { get; set; }
    public List<Package> Packages { get; set; } = new();
    public int? SelectedPackageId { get; set; }

    [Range(1, 1000, ErrorMessage = "Voer minimaal 1 ticket in.")]
    [Display(Name = "Aantal tickets")]
    public int TicketQuantity { get; set; } = 1;
}
