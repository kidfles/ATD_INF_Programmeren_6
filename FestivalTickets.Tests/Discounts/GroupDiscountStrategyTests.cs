using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;

namespace FestivalTickets.Tests.Discounts;

public sealed class GroupDiscountStrategyTests
{
    private readonly GroupDiscountStrategy _sut = new();

    [Fact]
    public void ApplyDiscount_FourTickets_NoDiscount()
    {
        var context = CreateContext(
            currentTotal: 300m,
            ticketQuantity: 4,
            (1, ItemType.Parking, 50m, 2)); // extra = 100, ticket part = 200

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(300m, result);
    }

    [Fact]
    public void ApplyDiscount_FiveTickets_AppliesTwentyPercentOnTicketPortion()
    {
        var context = CreateContext(
            currentTotal: 300m,
            ticketQuantity: 5,
            (1, ItemType.Parking, 50m, 2)); // ticket part = 200

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(260m, result);
    }

    [Fact]
    public void ApplyDiscount_TenTickets_AppliesTwentyPercentOnTicketPortion()
    {
        var context = CreateContext(
            currentTotal: 300m,
            ticketQuantity: 10,
            (1, ItemType.Parking, 50m, 2)); // ticket part = 200

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(260m, result);
    }

    private static DiscountContext CreateContext(
        decimal currentTotal,
        int ticketQuantity,
        params (int ItemId, ItemType ItemType, decimal UnitPrice, int Quantity)[] extraItems)
    {
        return new DiscountContext
        {
            BaseTotal = currentTotal,
            CurrentTotal = currentTotal,
            TicketQuantity = ticketQuantity,
            BookingDate = new DateTime(2026, 1, 1),
            FestivalStartDate = new DateOnly(2026, 3, 1),
            HasLoyaltyCard = false,
            ExtraItems = extraItems.ToList()
        };
    }
}
