using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;
using Moq;

namespace FestivalTickets.Tests.Discounts;

public sealed class DiscountCalculatorTests
{
    [Fact]
    public void Calculate_WhenNoDiscountsApply_FinalEqualsBase()
    {
        var sut = new DiscountCalculator();
        var context = new DiscountContext
        {
            BaseTotal = 100m,
            CurrentTotal = 100m,
            TicketQuantity = 1,
            BookingDate = new DateTime(2026, 8, 1),
            FestivalStartDate = new DateOnly(2026, 9, 1),
            HasLoyaltyCard = false,
            ExtraItems = new List<(int, ItemType, decimal, int)>()
        };

        var result = sut.Calculate(context);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void Calculate_TShirtAndLoyalty_DiscountsStack()
    {
        var sut = new DiscountCalculator();
        var context = new DiscountContext
        {
            BaseTotal = 80m,
            CurrentTotal = 80m,
            TicketQuantity = 1,
            BookingDate = new DateTime(2026, 8, 25),
            FestivalStartDate = new DateOnly(2026, 9, 1),
            HasLoyaltyCard = true,
            ExtraItems =
            [
                (1, ItemType.Merchandise, 20m, 4)
            ]
        };

        var result = sut.Calculate(context);

        // 80 - 20 (t-shirt) = 60; 60 * 0.9 (loyalty) = 54
        Assert.Equal(54m, result);
    }

    [Fact]
    public void Calculate_WithInjectedMockStrategy_UsesInjectedStrategy()
    {
        var strategyMock = new Mock<IDiscountStrategy>();
        strategyMock.SetupGet(s => s.Name).Returns("Mock 50%");
        strategyMock
            .Setup(s => s.ApplyDiscount(It.IsAny<DiscountContext>()))
            .Returns<DiscountContext>(ctx => ctx.CurrentTotal * 0.5m);

        var sut = new DiscountCalculator(new List<IDiscountStrategy> { strategyMock.Object });
        var context = new DiscountContext
        {
            BaseTotal = 200m,
            CurrentTotal = 200m
        };

        var result = sut.Calculate(context);

        Assert.Equal(100m, result);
        strategyMock.Verify(s => s.ApplyDiscount(It.IsAny<DiscountContext>()), Times.Once);
    }

    [Fact]
    public void Calculate_WhenDiscountGoesNegative_ReturnsZero()
    {
        var strategyMock = new Mock<IDiscountStrategy>();
        strategyMock.SetupGet(s => s.Name).Returns("Negative");
        strategyMock
            .Setup(s => s.ApplyDiscount(It.IsAny<DiscountContext>()))
            .Returns(-25m);

        var sut = new DiscountCalculator(new List<IDiscountStrategy> { strategyMock.Object });
        var context = new DiscountContext
        {
            BaseTotal = 10m,
            CurrentTotal = 10m
        };

        var result = sut.Calculate(context);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void GetBreakdown_WhenDiscountsApply_ReturnsOnlyAppliedDiscounts()
    {
        var sut = new DiscountCalculator();
        var context = new DiscountContext
        {
            BaseTotal = 80m,
            CurrentTotal = 80m,
            TicketQuantity = 1,
            BookingDate = new DateTime(2026, 8, 25),
            FestivalStartDate = new DateOnly(2026, 9, 1),
            HasLoyaltyCard = true,
            ExtraItems =
            [
                (1, ItemType.Merchandise, 20m, 4)
            ]
        };

        var breakdown = sut.GetBreakdown(context);

        Assert.Equal(2, breakdown.Count);
        Assert.Contains(breakdown, x => x.Name.Contains("T-shirt") && x.Saving == 20m);
        Assert.Contains(breakdown, x => x.Name.Contains("Loyaliteitskaart") && x.Saving == 6m);
    }

    [Fact]
    public void GetBreakdown_WhenNoDiscountsApply_ReturnsEmptyList()
    {
        var sut = new DiscountCalculator();
        var context = new DiscountContext
        {
            BaseTotal = 120m,
            CurrentTotal = 120m,
            TicketQuantity = 1,
            BookingDate = new DateTime(2026, 8, 1),
            FestivalStartDate = new DateOnly(2026, 8, 15),
            HasLoyaltyCard = false,
            ExtraItems = new List<(int, ItemType, decimal, int)>()
        };

        var breakdown = sut.GetBreakdown(context);

        Assert.Empty(breakdown);
    }
}
