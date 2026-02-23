using System;
using System.Linq;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Koop 4 t-shirts (Merchandise items), krijg 1 gratis.
/// Het goedkoopste t-shirt in de selectie is de gratis variant.
/// Toegepast per 4 gekochte items: als je er 8 koopt → 2 gratis, etc.
/// </summary>
public sealed class TShirtDiscountStrategy : IDiscountStrategy
{
    public string Name => "T-shirt korting (koop 4, krijg 1 gratis)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        // Zoek alle Merchandise items in extra items
        var tshirtItems = context.ExtraItems
            .Where(e => e.ItemType == ItemType.Merchandise)
            .ToList();

        int totalTShirts = tshirtItems.Sum(e => e.Quantity);
        int freeCount = totalTShirts / 4; // integer deling: elke 4 gekocht → 1 gratis

        if (freeCount == 0) return context.CurrentTotal;

        // Geef de goedkoopste items eerst weg
        decimal freeValue = 0m;
        int stillFree = freeCount;

        foreach (var item in tshirtItems.OrderBy(e => e.UnitPrice))
        {
            if (stillFree <= 0) break;
            int canTake = Math.Min(item.Quantity, stillFree);
            freeValue += canTake * item.UnitPrice;
            stillFree -= canTake;
        }

        return context.CurrentTotal - freeValue;
    }
}
