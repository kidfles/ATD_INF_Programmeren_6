using FestivalTickets.Domain.Discounts;

namespace FestivalTickets.Tests.Discounts;

public sealed class LoyaltyCardDiscountStrategyTests
{
    private readonly LoyaltyCardDiscountStrategy _sut = new();

    [Fact]
    public void ApplyDiscount_NoLoyaltyCard_NoDiscount()
    {
        var context = new DiscountContext
        {
            BaseTotal = 200m,
            CurrentTotal = 200m,
            HasLoyaltyCard = false
        };

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(200m, result);
    }

    [Fact]
    public void ApplyDiscount_WithLoyaltyCard_TenPercentOff()
    {
        var context = new DiscountContext
        {
            BaseTotal = 200m,
            CurrentTotal = 200m,
            HasLoyaltyCard = true
        };

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(180m, result);
    }

    [Fact]
    public void ApplyDiscount_AppliesToAlreadyDiscountedTotal()
    {
        var context = new DiscountContext
        {
            BaseTotal = 200m,
            CurrentTotal = 150m,
            HasLoyaltyCard = true
        };

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(135m, result);
    }
}
