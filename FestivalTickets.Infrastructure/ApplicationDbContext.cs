using FestivalTickets.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure;

// This acts as the bridge to our database.
public sealed class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    // The tables in our database.
    public DbSet<Festival> Festivals => Set<Festival>();
    public DbSet<Package>  Packages  => Set<Package>();
    public DbSet<Item>     Items     => Set<Item>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // MOET als eerste worden aangeroepen — stelt Identity-tabellen in

        // ── PackageItem samengestelde sleutel ──────────────────────────────
        b.Entity<PackageItem>().HasKey(pi => new { pi.PackageId, pi.ItemId });

        // ── Relaties ───────────────────────────────────────────────────────
        b.Entity<Package>()
            .HasOne(p => p.Festival)
            .WithMany(f => f.Packages)
            .HasForeignKey(p => p.FestivalId);

        b.Entity<PackageItem>()
            .HasOne(pi => pi.Package)
            .WithMany(p => p.PackageItems)
            .HasForeignKey(pi => pi.PackageId);

        b.Entity<PackageItem>()
            .HasOne(pi => pi.Item)
            .WithMany()
            .HasForeignKey(pi => pi.ItemId);

        b.Entity<Customer>()
            .HasIndex(c => c.UserId)
            .IsUnique();
        b.Entity<Customer>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(x => x.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(x => x.CustId);

        b.Entity<Booking>()
            .HasOne(x => x.Package)
            .WithMany()
            .HasForeignKey(x => x.PackageId);

        b.Entity<BookingItem>()
            .HasOne(x => x.Booking)
            .WithMany(x => x.BookingItems)
            .HasForeignKey(x => x.BookingId);

        b.Entity<BookingItem>()
            .HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId);

        // Decimal precision for money
        b.Entity<Festival>().Property(x => x.BasicPrice).HasPrecision(18, 2);
        b.Entity<Item>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Booking>().Property(x => x.TotalPricePaid).HasPrecision(18, 2);

        // ── Datum kolommen ─────────────────────────────────────────────────
        b.Entity<Festival>().Property(x => x.StartDate).HasColumnType("date");
        b.Entity<Festival>().Property(x => x.EndDate).HasColumnType("date");

        // ── Indexen ────────────────────────────────────────────────────────
        b.Entity<Festival>().HasIndex(x => x.Name);
        b.Entity<Item>().HasIndex(x => new { x.ItemType, x.Name });

        // ── Schakel ALLE cascade deletes uit ───────────────────────────────
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // ── SEED DATA (niet-identiteit) ────────────────────────────────────
        SeedFestivals(b);
        SeedPackages(b);
        SeedItems(b);
        SeedPackageItems(b);
    }

    private static void SeedFestivals(ModelBuilder b)
    {
        b.Entity<Festival>().HasData(
            new Festival
            {
                Id = 1,
                Name = "Intents Festival",
                Place = "Oisterwijk",
                Logo = "/img/logos/intents.png",
                Description = "Een driedaags allround festival met vooral focus op de hardere stijlen.",
                BasicPrice = 50.00m,
                StartDate = new DateOnly(2026, 6, 5),
                EndDate   = new DateOnly(2026, 6, 7)
            },
            new Festival
            {
                Id = 2,
                Name = "Pinkpop",
                Place = "Landgraaf",
                Logo = "/img/logos/pinkpop.png",
                Description = "Het oudste pop-festival van Nederland met een breed aanbod.",
                BasicPrice = 75.00m,
                StartDate = new DateOnly(2026, 12, 18),
                EndDate   = new DateOnly(2026, 12, 20)
            }
        );
    }

    private static void SeedPackages(ModelBuilder b)
    {
        b.Entity<Package>().HasData(
            // Festival 1 — Intents
            new Package { Id = 1, FestivalId = 1, Name = "Budget Deal" },
            new Package { Id = 2, FestivalId = 1, Name = "Camping Comfort" },
            new Package { Id = 3, FestivalId = 1, Name = "Ultimate Experience" },
            // Festival 2 — Pinkpop
            new Package { Id = 4, FestivalId = 2, Name = "Day Tripper" },
            new Package { Id = 5, FestivalId = 2, Name = "Weekend Warrior" },
            new Package { Id = 6, FestivalId = 2, Name = "VIP All-In" }
        );
    }

    private static void SeedItems(ModelBuilder b)
    {
        // 3 items per ItemType = 18 totaal
        b.Entity<Item>().HasData(
            // Camping (0)
            new Item { Id = 1,  ItemType = ItemType.Camping,         Name = "Campingspot Small",          Price = 25.00m },
            new Item { Id = 2,  ItemType = ItemType.Camping,         Name = "Campingspot Large",          Price = 40.00m },
            new Item { Id = 3,  ItemType = ItemType.Camping,         Name = "Glamping Tipi (2 nights)",   Price = 120.00m },
            // Food_and_Drinks (1)
            new Item { Id = 4,  ItemType = ItemType.Food_and_Drinks, Name = "Meal Voucher",               Price = 12.50m },
            new Item { Id = 5,  ItemType = ItemType.Food_and_Drinks, Name = "Drink Pack (10 tokens)",     Price = 15.00m },
            new Item { Id = 6,  ItemType = ItemType.Food_and_Drinks, Name = "Breakfast Combo",            Price = 9.50m },
            // Parking (2)
            new Item { Id = 7,  ItemType = ItemType.Parking,         Name = "Parking Day Pass",           Price = 10.00m },
            new Item { Id = 8,  ItemType = ItemType.Parking,         Name = "Parking Weekend Pass",       Price = 25.00m },
            new Item { Id = 9,  ItemType = ItemType.Parking,         Name = "VIP Parking (48h)",          Price = 50.00m },
            // Merchandise (3)
            new Item { Id = 10, ItemType = ItemType.Merchandise,     Name = "Festival T-shirt size L",    Price = 20.00m },
            new Item { Id = 11, ItemType = ItemType.Merchandise,     Name = "Festival Hoodie",            Price = 45.00m },
            new Item { Id = 12, ItemType = ItemType.Merchandise,     Name = "Baseball Cap",               Price = 15.00m },
            // VIPAccess (4)
            new Item { Id = 13, ItemType = ItemType.VIPAccess,       Name = "VIP Lounge Access",          Price = 60.00m },
            new Item { Id = 14, ItemType = ItemType.VIPAccess,       Name = "Meet & Greet Zone",          Price = 80.00m },
            new Item { Id = 15, ItemType = ItemType.VIPAccess,       Name = "VIP Backstage Tour",         Price = 150.00m },
            // Other (5)
            new Item { Id = 16, ItemType = ItemType.Other,           Name = "Locker Rental",              Price = 15.00m },
            new Item { Id = 17, ItemType = ItemType.Other,           Name = "Powerbank Rental",           Price = 8.00m },
            new Item { Id = 18, ItemType = ItemType.Other,           Name = "Rain Poncho",                Price = 5.00m }
        );
    }

    private static void SeedPackageItems(ModelBuilder b)
    {
        // Packages 1 & 4: geen items (alleen toegang)
        // Packages 2 & 5: alleen T-shirt
        b.Entity<PackageItem>().HasData(
            new PackageItem { PackageId = 2, ItemId = 10, Quantity = 1 }, // Camping Comfort → T-shirt
            new PackageItem { PackageId = 5, ItemId = 10, Quantity = 1 }, // Weekend Warrior → T-shirt
            // Packages 3 & 6: meerdere items
            new PackageItem { PackageId = 3, ItemId = 3,  Quantity = 1 }, // Ultimate → Glamping Tipi
            new PackageItem { PackageId = 3, ItemId = 13, Quantity = 1 }, // Ultimate → VIP Lounge
            new PackageItem { PackageId = 3, ItemId = 5,  Quantity = 2 }, // Ultimate → 2x Drink Pack
            new PackageItem { PackageId = 6, ItemId = 9,  Quantity = 1 }, // VIP All-In → VIP Parking
            new PackageItem { PackageId = 6, ItemId = 14, Quantity = 1 }, // VIP All-In → Meet & Greet
            new PackageItem { PackageId = 6, ItemId = 4,  Quantity = 3 }  // VIP All-In → 3x Meal Voucher
        );
    }
}
