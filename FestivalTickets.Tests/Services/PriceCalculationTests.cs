using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;

namespace FestivalTickets.Tests.Services;

public sealed class PriceCalculationTests
{
    [Fact]
    public void FullPricePipeline_BaseToDiscountsToFinal_IsCalculatedCorrectly()
    {
        var package = new Package
        {
            Id = 1,
            Name = "Weekend Plus",
            FestivalId = 1,
            Festival = new Festival
            {
                Id = 1,
                Name = "Lowlands",
                Place = "Biddinghuizen",
                BasicPrice = 100m,
                StartDate = new DateOnly(2026, 8, 20),
                EndDate = new DateOnly(2026, 8, 22)
            },
            PackageItems =
            [
                new PackageItem
                {
                    PackageId = 1,
                    ItemId = 10,
                    Quantity = 1,
                    Item = new Item { Id = 10, Name = "Campingspot", ItemType = ItemType.Camping, Price = 20m }
                }
            ]
        };

        var ticketQuantity = 5;
        var extraItems = new List<(int ItemId, ItemType ItemType, decimal UnitPrice, int Quantity)>
        {
            (20, ItemType.Parking, 40m, 2) // extras = 80
        };

        decimal packageItemsCost = package.PackageItems.Sum(pi => pi.Item.Price * pi.Quantity); // 20
        decimal baseTotal = (package.Festival!.BasicPrice + packageItemsCost) * ticketQuantity // 600
                            + extraItems.Sum(x => x.UnitPrice * x.Quantity); // +80 = 680

        var context = new DiscountContext
        {
            BaseTotal = baseTotal,
            CurrentTotal = baseTotal,
            TicketQuantity = ticketQuantity,
            BookingDate = new DateTime(2026, 1, 1), // early bird applies
            FestivalStartDate = package.Festival.StartDate,
            HasLoyaltyCard = true,
            ExtraItems = extraItems
        };

        var calculator = new DiscountCalculator();
        var finalTotal = calculator.Calculate(context);

        // Expected with current strategy order/behavior:
        // Start 680
        // T-shirt: none => 680
        // Early bird: ticket part 600, 15% = 90 => 590
        // Group: ticket part (590 - 80) = 510, 20% = 102 => 488
        // Loyalty: 10% => 439.20
        Assert.Equal(439.20m, finalTotal);
    }
}
