using System;
using System.Collections.Generic;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Orkestreert alle actieve kortingsstrategieën.
/// Strategieën worden in volgorde toegepast: T-shirt → Vroegboek → Groep → Loyalty Card.
/// De output van elke strategie wordt als CurrentTotal doorgegeven aan de volgende.
/// </summary>
public sealed class DiscountCalculator
{
    private readonly IReadOnlyList<IDiscountStrategy> _strategies;

    // Standaard constructor: alle 4 de strategieën actief, in de juiste volgorde
    public DiscountCalculator()
    {
        _strategies = new List<IDiscountStrategy>
        {
            new TShirtDiscountStrategy(),
            new EarlyBirdDiscountStrategy(),
            new GroupDiscountStrategy(),
            new LoyaltyCardDiscountStrategy()
        };
    }

    // Constructor voor testen: injecteer aangepaste strategieën
    public DiscountCalculator(IReadOnlyList<IDiscountStrategy> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    /// <summary>
    /// Berekent de uiteindelijke prijs na het toepassen van alle kortingsstrategieën in volgorde.
    /// </summary>
    public decimal Calculate(DiscountContext context)
    {
        context.CurrentTotal = context.BaseTotal;

        foreach (var strategy in _strategies)
        {
            context.CurrentTotal = strategy.ApplyDiscount(context);
        }

        return Math.Max(0m, context.CurrentTotal); // prijs kan nooit negatief worden
    }

    /// <summary>
    /// Geeft een overzicht terug van welke kortingen zijn toegepast en hun besparingen.
    /// Handig om weer te geven op de factuur (Stap 5).
    /// </summary>
    public IList<(string Name, decimal Saving)> GetBreakdown(DiscountContext context)
    {
        var result = new List<(string, decimal)>();
        decimal running = context.BaseTotal;

        foreach (var strategy in _strategies)
        {
            context.CurrentTotal = running;
            decimal after = strategy.ApplyDiscount(context);
            decimal saving = running - after;
            if (saving > 0)
                result.Add((strategy.Name, saving));
            running = after;
        }

        return result;
    }
}
