using System.Collections.Generic;
using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

// Holds all the info to show a package's details on the screen.
public sealed class PackageDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FestivalId { get; set; }
    public string FestivalName { get; set; } = string.Empty;
    public string? FestivalPlace { get; set; }
    public IReadOnlyCollection<PackageDetailsItemViewModel> Items { get; set; } = new List<PackageDetailsItemViewModel>();
    public decimal TotalPrice { get; set; }
}

// A little helper to show one line item in the package details.
public sealed class PackageDetailsItemViewModel
{
    public string ItemName { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
