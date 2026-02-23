namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// 10% korting op de totaalprijs voor klanten die een Loyalty Card hebben (Identity claim).
/// Toegepast na alle andere kortingen.
/// </summary>
public sealed class LoyaltyCardDiscountStrategy : IDiscountStrategy
{
    public string Name => "Loyaliteitskaart korting (10%)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        if (!context.HasLoyaltyCard) return context.CurrentTotal;

        return context.CurrentTotal * 0.90m;
    }
}
