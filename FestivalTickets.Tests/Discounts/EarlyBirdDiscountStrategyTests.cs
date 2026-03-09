using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;

namespace FestivalTickets.Tests.Discounts;

public sealed class EarlyBirdDiscountStrategyTests
{
    private readonly EarlyBirdDiscountStrategy _sut = new();

    [Fact]
    public void ApplyDiscount_ExactlyFiveMonthsBefore_AppliesFifteenPercent()
    {
        var festivalStart = new DateOnly(2026, 10, 1);
        var bookingDate = festivalStart.AddMonths(-5).ToDateTime(TimeOnly.MinValue);
        var context = CreateContext(200m, bookingDate, festivalStart);

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(170m, result);
    }

    [Fact]
    public void ApplyDiscount_SixMonthsBefore_AppliesFifteenPercent()
    {
        var festivalStart = new DateOnly(2026, 10, 1);
        var bookingDate = festivalStart.AddMonths(-6).ToDateTime(TimeOnly.MinValue);
        var context = CreateContext(200m, bookingDate, festivalStart);

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(170m, result);
    }

    [Fact]
    public void ApplyDiscount_FourMonthsBefore_NoDiscount()
    {
        var festivalStart = new DateOnly(2026, 10, 1);
        var bookingDate = festivalStart.AddMonths(-4).ToDateTime(TimeOnly.MinValue);
        var context = CreateContext(200m, bookingDate, festivalStart);

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(200m, result);
    }

    [Fact]
    public void ApplyDiscount_DayBeforeFestival_NoDiscount()
    {
        var festivalStart = new DateOnly(2026, 10, 1);
        var bookingDate = festivalStart.AddDays(-1).ToDateTime(TimeOnly.MinValue);
        var context = CreateContext(200m, bookingDate, festivalStart);

        var result = _sut.ApplyDiscount(context);

        Assert.Equal(200m, result);
    }

    [Fact]
    public void ApplyDiscount_DiscountsOnlyTicketPortion_NotExtraItems()
    {
        var festivalStart = new DateOnly(2026, 12, 1);
        var bookingDate = festivalStart.AddMonths(-6).ToDateTime(TimeOnly.MinValue);

        var context = new DiscountContext
        {
            BaseTotal = 300m,
            CurrentTotal = 300m,
            TicketQuantity = 1,
            BookingDate = bookingDate,
            FestivalStartDate = festivalStart,
            HasLoyaltyCard = false,
            ExtraItems =
            [
                (1, ItemType.Parking, 50m, 2) // 100 extra items
            ]
        };

        var result = _sut.ApplyDiscount(context);

        // Ticket portion is 200 => 15% discount = 30
        Assert.Equal(270m, result);
    }

    private static DiscountContext CreateContext(decimal total, DateTime bookingDate, DateOnly festivalStartDate)
    {
        return new DiscountContext
        {
            BaseTotal = total,
            CurrentTotal = total,
            TicketQuantity = 1,
            BookingDate = bookingDate,
            FestivalStartDate = festivalStartDate,
            HasLoyaltyCard = false,
            ExtraItems = new List<(int, ItemType, decimal, int)>()
        };
    }
}
