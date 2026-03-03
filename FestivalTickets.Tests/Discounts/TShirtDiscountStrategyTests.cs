using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;

namespace FestivalTickets.Tests.Discounts;

public sealed class TShirtDiscountStrategyTests
{
    private readonly TShirtDiscountStrategy _sut = new();

    [Fact]
    public void ApplyDiscount_ZeroTShirts_NoDiscount()
    {
        var context = CreateContext(
            currentTotal: 200m,
            (1, ItemType.Parking, 20m, 2));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(200m, result);
    }

    [Fact]
    public void ApplyDiscount_ThreeTShirts_NoDiscount()
    {
        var context = CreateContext(
            currentTotal: 60m,
            (1, ItemType.Merchandise, 20m, 3));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(60m, result);
    }

    [Fact]
    public void ApplyDiscount_FourEqualTShirts_OneFree()
    {
        var context = CreateContext(
            currentTotal: 80m,
            (1, ItemType.Merchandise, 20m, 4));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(60m, result);
    }

    [Fact]
    public void ApplyDiscount_EightTShirts_TwoFree()
    {
        var context = CreateContext(
            currentTotal: 160m,
            (1, ItemType.Merchandise, 20m, 8));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(120m, result);
    }

    [Fact]
    public void ApplyDiscount_FourDifferentPrices_CheapestFree()
    {
        var context = CreateContext(
            currentTotal: 70m,
            (1, ItemType.Merchandise, 10m, 1),
            (2, ItemType.Merchandise, 15m, 1),
            (3, ItemType.Merchandise, 20m, 1),
            (4, ItemType.Merchandise, 25m, 1));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(60m, result);
    }

    [Fact]
    public void ApplyDiscount_TwoTShirtsAndOneHoodie_NoDiscount()
    {
        var context = CreateContext(
            currentTotal: 95m,
            (1, ItemType.Merchandise, 20m, 2),
            (2, ItemType.Merchandise, 55m, 1));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(95m, result);
    }

    [Fact]
    public void ApplyDiscount_FourTShirtsAndParking_DiscountAppliesOnlyToMerchandise()
    {
        var context = CreateContext(
            currentTotal: 90m,
            (1, ItemType.Merchandise, 20m, 4),
            (2, ItemType.Parking, 10m, 1));

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(70m, result);
    }

    private static DiscountContext CreateContext(
        decimal currentTotal,
        params (int ItemId, ItemType ItemType, decimal UnitPrice, int Quantity)[] extraItems)
    {
        return new DiscountContext
        {
            BaseTotal = currentTotal,
            CurrentTotal = currentTotal,
            ExtraItems = extraItems.ToList(),
            TicketQuantity = 1,
            BookingDate = new DateTime(2026, 1, 1),
            FestivalStartDate = new DateOnly(2026, 3, 1),
            HasLoyaltyCard = false
        };
    }
}
