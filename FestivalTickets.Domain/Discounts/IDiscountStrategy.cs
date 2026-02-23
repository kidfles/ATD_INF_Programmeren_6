namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Strategy Pattern contract. Elke kortingsregel implementeert deze interface.
/// ApplyDiscount ontvangt het huidige lopende totaal en context, en retourneert het nieuwe totaal.
/// </summary>
public interface IDiscountStrategy
{
    string Name { get; }

    /// <summary>
    /// Berekent de kortingsprijs.
    /// </summary>
    /// <param name="context">Alle gegevens die nodig zijn om de korting te evalueren.</param>
    /// <returns>De totaalprijs nadat deze korting is toegepast.</returns>
    decimal ApplyDiscount(DiscountContext context);
}
