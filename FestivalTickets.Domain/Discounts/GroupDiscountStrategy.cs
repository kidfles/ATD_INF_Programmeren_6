using System.Linq;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// 20% korting op ticketprijs bij boeken van 5 of meer tickets.
/// </summary>
public sealed class GroupDiscountStrategy : IDiscountStrategy
{
    public string Name => "Groepskorting (20% bij 5+ tickets)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        if (context.TicketQuantity < 5) return context.CurrentTotal;

        decimal extraItemTotal = context.ExtraItems.Sum(e => e.UnitPrice * e.Quantity);
        decimal ticketTotal = context.CurrentTotal - extraItemTotal;
        decimal discount = ticketTotal * 0.20m;

        return context.CurrentTotal - discount;
    }
}
