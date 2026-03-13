using System;
using System.Linq;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// 15% korting op de ticketprijs wanneer de boekingsdatum minstens 5 maanden voor de start van het festival is.
/// Geldt alleen voor het ticketgedeelte (BasicPrice × aantal), niet voor extra items.
/// </summary>
public sealed class EarlyBirdDiscountStrategy : IDiscountStrategy
{
    public string Name => "Vroegboekkorting (15%)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        var festivalStart = context.FestivalStartDate.ToDateTime(TimeOnly.MinValue);
        var monthsDiff = (festivalStart - context.BookingDate).TotalDays / 30.0;

        if (monthsDiff < 5.0) return context.CurrentTotal;

        // 15% korting op alleen het ticketgedeelte (BasicPrice × ticketQuantity)
        // We hebben hier geen BasicPrice afzonderlijk, dus deze zit al in CurrentTotal
        // De strategie ontvangt CurrentTotal wat op dit punt nog gelijk is aan BaseTotal
        // Ticketprijs = BaseTotal - som van alle extra item kosten
        decimal extraItemTotal = context.ExtraItems.Sum(e => e.UnitPrice * e.Quantity);
        decimal ticketTotal = Math.Max(0m, context.CurrentTotal - extraItemTotal);
        decimal discount = ticketTotal * 0.15m;

        return context.CurrentTotal - discount;
    }
}
