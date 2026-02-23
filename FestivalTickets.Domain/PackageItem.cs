using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

// Link between a package and an item. Says "This package has 5 of these tents".
public sealed class PackageItem
{
    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    [Range(0, 1000)]
    public int Quantity { get; set; }
}


