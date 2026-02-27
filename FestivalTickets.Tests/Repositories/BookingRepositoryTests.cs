using FestivalTickets.Domain;

namespace FestivalTickets.Tests.Repositories;

public sealed class BookingRepositoryTests
{
    [Fact]
    public async Task AddAndGetById_ReturnsBooking()
    {
        var repo = new InMemoryBookingRepository();
        var booking = new Booking
        {
            PackageId = 1,
            Quantity = 2,
            CustId = 10,
            BookingDate = new DateTime(2026, 2, 1),
            TotalPricePaid = 299.99m
        };

        await repo.AddAsync(booking);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(1);

        Assert.NotNull(loaded);
        Assert.Equal(10, loaded.CustId);
        Assert.Equal(299.99m, loaded.TotalPricePaid);
    }

    [Fact]
    public async Task GetByCustomerId_ReturnsOnlyMatchingBookings()
    {
        var repo = new InMemoryBookingRepository();

        await repo.AddAsync(new Booking { PackageId = 1, Quantity = 1, CustId = 1, BookingDate = DateTime.Now, TotalPricePaid = 100m });
        await repo.AddAsync(new Booking { PackageId = 2, Quantity = 1, CustId = 2, BookingDate = DateTime.Now, TotalPricePaid = 120m });
        await repo.AddAsync(new Booking { PackageId = 3, Quantity = 1, CustId = 1, BookingDate = DateTime.Now, TotalPricePaid = 130m });

        var result = (await repo.GetByCustomerIdAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(1, b.CustId));
    }

    [Fact]
    public async Task Delete_RemovesBooking()
    {
        var repo = new InMemoryBookingRepository();

        await repo.AddAsync(new Booking { PackageId = 1, Quantity = 1, CustId = 1, BookingDate = DateTime.Now, TotalPricePaid = 99m });
        await repo.DeleteAsync(1);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(1);

        Assert.Null(loaded);
    }
}
