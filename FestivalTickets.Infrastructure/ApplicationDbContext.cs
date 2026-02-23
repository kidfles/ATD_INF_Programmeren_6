using FestivalTickets.Domain;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure;

// This acts as the bridge to our database.
public sealed class ApplicationDbContext : DbContext
{
    // The tables in our database.
    public DbSet<Festival> Festivals => Set<Festival>();
    public DbSet<Package>  Packages  => Set<Package>();
    public DbSet<Item>     Items     => Set<Item>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // This is where we configure how the database looks (relationships, constraints, etc).
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Making sure we don't have duplicate links between packages and items.
        b.Entity<PackageItem>().HasKey(pi => new { pi.PackageId, pi.ItemId });

        // Relationships
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

        // Decimal precision for money
        b.Entity<Festival>().Property(x => x.BasicPrice).HasPrecision(18, 2);
        b.Entity<Item>().Property(x => x.Price).HasPrecision(18, 2);

        // Map DateOnly to date (SQL Server)
        b.Entity<Festival>().Property(x => x.StartDate).HasColumnType("date");
        b.Entity<Festival>().Property(x => x.EndDate).HasColumnType("date");

        // Indexes
        b.Entity<Festival>()
            .HasIndex(x => x.Name);
        b.Entity<Item>()
            .HasIndex(x => new { x.ItemType, x.Name });

        // Disable cascade deletes globally
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // Seed data: putting some initial info into the database so it's not empty.
        b.Entity<Festival>().HasData(new Festival
        {
            Id = 1,
            Name = "Lowlands",
            Place = "Biddinghuizen",
            Logo = "/img/logos/lowlands.svg",
            Description = "Three-day music festival.",
            BasicPrice = 199.00m,
            StartDate = new DateOnly(2025, 8, 22),
            EndDate = new DateOnly(2025, 8, 24)
        });

        b.Entity<Package>().HasData(
            new Package { Id = 1, FestivalId = 1, Name = "Weekend Basic" },
            new Package { Id = 2, FestivalId = 1, Name = "Weekend Plus" }
        );

        var items = new List<Item>();
        int id = 1;
        void AddItems(ItemType type, params (string name, decimal price)[] defs)
        {
            foreach (var d in defs)
            {
                items.Add(new Item { Id = id++, Name = d.name, ItemType = type, Price = d.price });
            }
        }

        AddItems(ItemType.Camping,
            ("Campingspot Small", 25m),
            ("Campingspot Large", 40m),
            ("Glamping Upgrade", 120m));

        AddItems(ItemType.Food_and_Drinks,
            ("Meal Voucher", 12.5m),
            ("Drink Pack", 15m),
            ("Breakfast", 9.5m));

        AddItems(ItemType.Parking,
            ("Parking Day", 10m),
            ("Parking Weekend", 25m),
            ("VIP Parking", 50m));

        AddItems(ItemType.Merchandise,
            ("T-Shirt", 30m),
            ("Hoodie", 55m),
            ("Poster", 12m));

        AddItems(ItemType.VIPAccess,
            ("VIP Day", 80m),
            ("VIP Weekend", 200m),
            ("Backstage Tour", 150m));

        AddItems(ItemType.Other,
            ("Locker", 15m),
            ("Powerbank Rental", 8m),
            ("Rain Poncho", 5m));

        b.Entity<Item>().HasData(items);
    }
}


