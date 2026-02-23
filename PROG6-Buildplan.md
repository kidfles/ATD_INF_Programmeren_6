# PROG6 — FestivalTickets: Complete Agent Build Plan

## How to use this document
This file is the **single source of truth** for building the PROG6 FestivalTickets application.
Feed phases to your coding agent one at a time. Each phase is self-contained.
Tell the agent: *"Follow the rules in this file. Execute Phase X now."*

---

## ⚠️ GLOBAL RULES (agent must follow at all times)

- **Language**: C# code, comments, DB column names → English. UI labels → Dutch (or English, both accepted per rubric).
- **Framework**: ASP.NET Core 9, MVC pattern, EF Core 9 Code First, MS SQL Server.
- **No cascade delete** anywhere. All FK `DeleteBehavior` = `Restrict`.
- **No AJAX**, no React/Angular. Plain MVC postbacks only.
- **JavaScript** is allowed (own scripts) but only for UX polish, not for core logic.
- **No AJAX** for the Booking Wizard. Use `HttpContext.Session` to persist state.
- **Architecture**: Domain entities live in `FestivalTickets.Domain` class library. ViewModels in `FestivalTickets.Web/Models/`. Infrastructure/EF in `FestivalTickets.Infrastructure`.
- **ViewModels** must be used as the bridge between every Controller and every View. Never pass a domain entity directly to a View except for simple read-only detail pages where no form is needed.
- **All fields** must have validation: DataAnnotations on domain models, and additional ModelState checks in controllers where needed.
- **Design Pattern**: Discounts use the Strategy Pattern. Each discount is a class implementing `IDiscountStrategy`. A `DiscountCalculator` applies all active strategies.
- **Repository Pattern**: Define `IBookingRepository`, `IFestivalRepository`, `ICustomerRepository` interfaces in Domain. Implement them in Infrastructure. Register via DI in Program.cs. Use inMemory implementations for tests.
- **Identity**: Use `Microsoft.AspNetCore.Identity` with `IdentityUser`. Roles: `"Customer"` and `"Administrator"` (exact strings). Loyalty Card is stored as an Identity Claim with type `"LoyaltyCard"` and value `"true"`.
- **Session**: Enable `AddSession()` and `AddDistributedMemoryCache()` in Program.cs. Session key = `"BookingWizard"`. Serialize/deserialize the wizard state as JSON.
- **Seed data**: Applied via `HasData()` in `OnModelCreating`. Passwords seeded via `IHostedService` or `WebApplication` startup code (not HasData, because Identity hashes passwords at runtime).
- **Unit Tests**: Project `FestivalTickets.Tests` (xUnit). Use Moq for mocking. Use an `InMemoryRepository` implementation for integration-style tests. Aim for >75% coverage of business logic (discount strategies, price calculation, booking rules).
- **No breaking existing PROG5 functionality**. Festivals, Packages, Items, PackageItems CRUD all continue to work exactly as before.

---

## Project Structure (target state)

```
FestivalTickets.sln
├── FestivalTickets.Domain/               ← entities, enums, interfaces, strategy contracts
├── FestivalTickets.Infrastructure/       ← EF DbContext, repositories, migrations
├── FestivalTickets.Web/                  ← MVC app (controllers, views, viewmodels, wwwroot)
└── FestivalTickets.Tests/                ← xUnit tests (mocking, in-memory)
```

---

## Namespace convention
- Old (PROG5): `FestivalConfigurator.*`
- New (PROG6): `FestivalTickets.*`

When renaming, do a solution-wide find-and-replace of `FestivalConfigurator` → `FestivalTickets`.

---

---

# PHASE 0 — Project Setup & Rename

## Goal
Copy the PROG5 solution, rename everything to FestivalTickets, verify it still compiles and runs.

## Steps

1. **Copy** the entire `kidfles-atd_inf_programmeren_5` folder. Rename the copy to `PROG6-FestivalTickets`.

2. **Rename solution file**: `FestivalConfigurator.sln` → `FestivalTickets.sln`

3. **Rename project folders and .csproj files**:
   - `FestivalConfigurator.Domain/` → `FestivalTickets.Domain/`
   - `FestivalConfigurator.Domain.csproj` → `FestivalTickets.Domain.csproj`
   - `FestivalConfigurator.Infrastructure/` → `FestivalTickets.Infrastructure/`
   - `FestivalConfigurator.Infrastructure.csproj` → `FestivalTickets.Infrastructure.csproj`
   - `FestivalConfigurator.Web/` → `FestivalTickets.Web/`
   - `FestivalConfigurator.Web.csproj` → `FestivalTickets.Web.csproj`

4. **Update .sln file**: Open `FestivalTickets.sln` in a text editor and replace all occurrences of `FestivalConfigurator` with `FestivalTickets`.

5. **Solution-wide namespace rename**: In all `.cs` and `.cshtml` files, replace:
   - `namespace FestivalConfigurator` → `namespace FestivalTickets`
   - `using FestivalConfigurator` → `using FestivalTickets`
   - `@model FestivalConfigurator` → `@model FestivalTickets`
   - `@using FestivalConfigurator` → `@using FestivalTickets`

6. **Update `appsettings.Development.json`**: Change `Database=FestivalConfigurator` → `Database=FestivalTickets`.

7. **Update `Program.cs`**: Change `MigrationsAssembly("FestivalConfigurator.Infrastructure")` → `MigrationsAssembly("FestivalTickets.Infrastructure")`.

8. **Delete all existing Migrations** in `FestivalTickets.Infrastructure/Migrations/` — we will regenerate them in Phase 4 after schema changes.

9. **Add a new Test project**:
   ```
   dotnet new xunit -n FestivalTickets.Tests
   dotnet sln FestivalTickets.sln add FestivalTickets.Tests/FestivalTickets.Tests.csproj
   ```
   Add project references:
   ```
   dotnet add FestivalTickets.Tests reference FestivalTickets.Domain
   dotnet add FestivalTickets.Tests reference FestivalTickets.Infrastructure
   ```
   Add NuGet packages to Tests project:
   ```
   dotnet add FestivalTickets.Tests package Moq
   dotnet add FestivalTickets.Tests package Microsoft.EntityFrameworkCore.InMemory
   ```

10. **Verify**: `dotnet build` from solution root. Must compile with 0 errors.

11. **Update README.md** with new project name and instructions.

---

---

# PHASE 1 — Domain Layer: New Entities

## Goal
Add `Customer`, `Booking`, `BookingItem` to the Domain project. Also add repository interfaces and the discount strategy contracts.

## Files to create/modify in `FestivalTickets.Domain/`

---

### `Customer.cs`

```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

/// <summary>
/// Represents a registered customer. Linked 1-to-1 with an AspNetUsers identity account.
/// </summary>
public sealed class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Achternaam")]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Foreign key to AspNetUsers.Id (string).</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
```

---

### `Booking.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

/// <summary>
/// A confirmed booking placed by a customer for a specific package (ticket).
/// </summary>
public sealed class Booking
{
    public int Id { get; set; }

    [Display(Name = "Pakket")]
    public int PackageId { get; set; }
    public Package? Package { get; set; }

    /// <summary>Number of tickets (copies of the package) booked.</summary>
    [Range(1, 1000)]
    [Display(Name = "Aantal tickets")]
    public int Quantity { get; set; }

    [Display(Name = "Klant")]
    public int CustId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Boekingsdatum")]
    public DateTime BookingDate { get; set; }

    /// <summary>Final total price after all discounts, stored at time of booking.</summary>
    [DataType(DataType.Currency)]
    [Display(Name = "Totaalprijs")]
    public decimal TotalPricePaid { get; set; }

    public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
}
```

---

### `BookingItem.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

/// <summary>
/// Junction table: extra items chosen by the customer during the booking wizard (Step 3).
/// Unlike PackageItems, these are freely chosen — no ItemType limit, any quantity.
/// </summary>
public sealed class BookingItem
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Aantal")]
    public int Quantity { get; set; }
}
```

---

### `Interfaces/IFestivalRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IFestivalRepository
{
    Task<IEnumerable<Festival>> GetAllAsync();
    Task<IEnumerable<Festival>> GetUpcomingAsync(DateOnly from, DateOnly to);
    Task<Festival?> GetByIdWithPackagesAsync(int id);
}
```

---

### `Interfaces/IPackageRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IPackageRepository
{
    Task<IEnumerable<Package>> GetByFestivalIdAsync(int festivalId);
    Task<Package?> GetByIdWithItemsAsync(int id);
}
```

---

### `Interfaces/IItemRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
}
```

---

### `Interfaces/IBookingRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id);
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId);
    Task AddAsync(Booking booking);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}
```

---

### `Interfaces/ICustomerRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalTickets.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByUserIdAsync(string userId);
    Task<Customer?> GetByIdAsync(int id);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task AddAsync(Customer customer);
    Task SaveChangesAsync();
}
```

---

### `Discounts/IDiscountStrategy.cs`

```csharp
namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Strategy Pattern contract. Each discount rule implements this interface.
/// ApplyDiscount receives the current running total and context, returns the new total.
/// </summary>
public interface IDiscountStrategy
{
    string Name { get; }

    /// <summary>
    /// Calculates the discounted price.
    /// </summary>
    /// <param name="context">All data needed to evaluate the discount.</param>
    /// <returns>The total price after this discount is applied.</returns>
    decimal ApplyDiscount(DiscountContext context);
}
```

---

### `Discounts/DiscountContext.cs`

```csharp
using System;
using System.Collections.Generic;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Carries all information needed for discount strategies to make decisions.
/// Passed to every IDiscountStrategy.ApplyDiscount() call.
/// </summary>
public sealed class DiscountContext
{
    /// <summary>Base price before discounts: Festival.BasicPrice × ticket quantity + all item prices.</summary>
    public decimal BaseTotal { get; set; }

    /// <summary>Running total: starts as BaseTotal, modified by each strategy in sequence.</summary>
    public decimal CurrentTotal { get; set; }

    /// <summary>How many tickets (package copies) are being booked.</summary>
    public int TicketQuantity { get; set; }

    /// <summary>The date the booking is being placed.</summary>
    public DateTime BookingDate { get; set; }

    /// <summary>The start date of the festival (for early-bird calculation).</summary>
    public DateOnly FestivalStartDate { get; set; }

    /// <summary>The extra items added by the customer in Step 3 (ItemId → quantity).</summary>
    public IList<(int ItemId, ItemType ItemType, decimal UnitPrice, int Quantity)> ExtraItems { get; set; }
        = new List<(int, ItemType, decimal, int)>();

    /// <summary>True if the customer has the Loyalty Card claim.</summary>
    public bool HasLoyaltyCard { get; set; }
}
```

---

### `Discounts/TShirtDiscountStrategy.cs`

```csharp
using System.Linq;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Buy 4 t-shirts (Merchandise items), get 1 free.
/// The cheapest t-shirt in the selection is the free one.
/// Applied per 4 purchased: if 8 bought → 2 free, etc.
/// </summary>
public sealed class TShirtDiscountStrategy : IDiscountStrategy
{
    public string Name => "T-shirt korting (koop 4, krijg 1 gratis)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        // Find all Merchandise items in extra items
        var tshirtItems = context.ExtraItems
            .Where(e => e.ItemType == ItemType.Merchandise)
            .ToList();

        int totalTShirts = tshirtItems.Sum(e => e.Quantity);
        int freeCount = totalTShirts / 4; // integer division: every 4 purchased → 1 free

        if (freeCount == 0) return context.CurrentTotal;

        // Give away the cheapest items first
        decimal freeValue = 0m;
        int stillFree = freeCount;

        foreach (var item in tshirtItems.OrderBy(e => e.UnitPrice))
        {
            if (stillFree <= 0) break;
            int canTake = Math.Min(item.Quantity, stillFree);
            freeValue += canTake * item.UnitPrice;
            stillFree -= canTake;
        }

        return context.CurrentTotal - freeValue;
    }
}
```

---

### `Discounts/EarlyBirdDiscountStrategy.cs`

```csharp
namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// 15% discount on ticket price when booking date is at least 5 months before festival start.
/// Only applies to the ticket price (BasicPrice × quantity), not extra items.
/// </summary>
public sealed class EarlyBirdDiscountStrategy : IDiscountStrategy
{
    public string Name => "Vroegboekkorting (15%)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        var festivalStart = context.FestivalStartDate.ToDateTime(TimeOnly.MinValue);
        var monthsDiff = (festivalStart - context.BookingDate).TotalDays / 30.0;

        if (monthsDiff < 5.0) return context.CurrentTotal;

        // 15% off the ticket portion only (BasicPrice × ticketQuantity)
        // We don't have BasicPrice here separately, so it is already in CurrentTotal
        // The strategy receives CurrentTotal which at this point still equals BaseTotal
        // Ticket price = BaseTotal - sum of all extra item costs
        decimal extraItemTotal = context.ExtraItems.Sum(e => e.UnitPrice * e.Quantity);
        decimal ticketTotal = context.CurrentTotal - extraItemTotal;
        decimal discount = ticketTotal * 0.15m;

        return context.CurrentTotal - discount;
    }
}
```

---

### `Discounts/GroupDiscountStrategy.cs`

```csharp
namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// 20% discount on ticket price when booking 5 or more tickets.
/// </summary>
public sealed class GroupDiscountStrategy : IDiscountStrategy
{
    public string Name => "Groepskorting (20% bij 5+ tickets)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        if (context.TicketQuantity < 5) return context.CurrentTotal;

        decimal extraItemTotal = context.ExtraItems.Sum(e => e.UnitPrice * e.Quantity);
        decimal ticketTotal = context.CurrentTotal - extraItemTotal;
        decimal discount = ticketTotal * 0.20m;

        return context.CurrentTotal - discount;
    }
}
```

---

### `Discounts/LoyaltyCardDiscountStrategy.cs`

```csharp
namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// 10% off the total price for customers who have a Loyalty Card (Identity claim).
/// Applied after all other discounts.
/// </summary>
public sealed class LoyaltyCardDiscountStrategy : IDiscountStrategy
{
    public string Name => "Loyaliteitskaart korting (10%)";

    public decimal ApplyDiscount(DiscountContext context)
    {
        if (!context.HasLoyaltyCard) return context.CurrentTotal;

        return context.CurrentTotal * 0.90m;
    }
}
```

---

### `Discounts/DiscountCalculator.cs`

```csharp
using System.Collections.Generic;
using System.Linq;

namespace FestivalTickets.Domain.Discounts;

/// <summary>
/// Orchestrates all active discount strategies.
/// Strategies are applied in order: T-shirt → Early Bird → Group → Loyalty Card.
/// The output of each strategy is fed as CurrentTotal into the next.
/// </summary>
public sealed class DiscountCalculator
{
    private readonly IReadOnlyList<IDiscountStrategy> _strategies;

    // Default constructor: all 4 strategies active, in correct order
    public DiscountCalculator()
    {
        _strategies = new List<IDiscountStrategy>
        {
            new TShirtDiscountStrategy(),
            new EarlyBirdDiscountStrategy(),
            new GroupDiscountStrategy(),
            new LoyaltyCardDiscountStrategy()
        };
    }

    // Constructor for testing: inject custom strategies
    public DiscountCalculator(IReadOnlyList<IDiscountStrategy> strategies)
    {
        _strategies = strategies;
    }

    /// <summary>
    /// Calculates the final price after applying all discount strategies in sequence.
    /// </summary>
    public decimal Calculate(DiscountContext context)
    {
        context.CurrentTotal = context.BaseTotal;

        foreach (var strategy in _strategies)
        {
            context.CurrentTotal = strategy.ApplyDiscount(context);
        }

        return Math.Max(0m, context.CurrentTotal); // price can never go negative
    }

    /// <summary>
    /// Returns a breakdown of which discounts were applied and their savings.
    /// Useful for displaying on the invoice (Step 5).
    /// </summary>
    public IList<(string Name, decimal Saving)> GetBreakdown(DiscountContext context)
    {
        var result = new List<(string, decimal)>();
        decimal running = context.BaseTotal;

        foreach (var strategy in _strategies)
        {
            context.CurrentTotal = running;
            decimal after = strategy.ApplyDiscount(context);
            decimal saving = running - after;
            if (saving > 0)
                result.Add((strategy.Name, saving));
            running = after;
        }

        return result;
    }
}
```

---

### Update `FestivalTickets.Domain.csproj`

No new NuGet packages needed in Domain. It is a pure class library.

---

---

# PHASE 2 — Infrastructure Layer: Extend DbContext + Identity + Repositories

## Goal
Add Identity, new tables (`Customers`, `Bookings`, `BookingItems`) to the DbContext. Implement repositories. Update seed data.

## NuGet packages to add to `FestivalTickets.Infrastructure`

```
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 9.*
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.*
```

---

## `ApplicationDbContext.cs` — FULL REPLACEMENT

```csharp
using FestivalTickets.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure;

/// <summary>
/// Main EF Core DbContext. Inherits from IdentityDbContext to include AspNetUsers, Roles, Claims etc.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Festival>     Festivals     => Set<Festival>();
    public DbSet<Package>      Packages      => Set<Package>();
    public DbSet<Item>         Items         => Set<Item>();
    public DbSet<PackageItem>  PackageItems  => Set<PackageItem>();
    public DbSet<Customer>     Customers     => Set<Customer>();
    public DbSet<Booking>      Bookings      => Set<Booking>();
    public DbSet<BookingItem>  BookingItems  => Set<BookingItem>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // MUST be called first — sets up Identity tables

        // ── PackageItem composite key ──────────────────────────────────────
        b.Entity<PackageItem>().HasKey(pi => new { pi.PackageId, pi.ItemId });

        // ── Relationships ──────────────────────────────────────────────────
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
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(bk => bk.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(bk => bk.CustId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Booking>()
            .HasOne(bk => bk.Package)
            .WithMany()
            .HasForeignKey(bk => bk.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<BookingItem>()
            .HasOne(bi => bi.Booking)
            .WithMany(bk => bk.BookingItems)
            .HasForeignKey(bi => bi.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<BookingItem>()
            .HasOne(bi => bi.Item)
            .WithMany()
            .HasForeignKey(bi => bi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Precision ──────────────────────────────────────────────────────
        b.Entity<Festival>().Property(x => x.BasicPrice).HasPrecision(18, 2);
        b.Entity<Item>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Booking>().Property(x => x.TotalPricePaid).HasPrecision(18, 2);

        // ── Date columns ───────────────────────────────────────────────────
        b.Entity<Festival>().Property(x => x.StartDate).HasColumnType("date");
        b.Entity<Festival>().Property(x => x.EndDate).HasColumnType("date");

        // ── Indexes ────────────────────────────────────────────────────────
        b.Entity<Festival>().HasIndex(x => x.Name);
        b.Entity<Item>().HasIndex(x => new { x.ItemType, x.Name });
        b.Entity<Customer>().HasIndex(x => x.UserId).IsUnique();

        // ── Disable ALL cascade deletes ────────────────────────────────────
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // ── SEED DATA (non-identity) ───────────────────────────────────────
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
        // 3 items per ItemType = 18 total
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
        // Packages 1 & 4: no items (admission only)
        // Packages 2 & 5: T-shirt only
        b.Entity<PackageItem>().HasData(
            new PackageItem { PackageId = 2, ItemId = 10, Quantity = 1 }, // Camping Comfort → T-shirt
            new PackageItem { PackageId = 5, ItemId = 10, Quantity = 1 }, // Weekend Warrior → T-shirt
            // Packages 3 & 6: multiple items
            new PackageItem { PackageId = 3, ItemId = 3,  Quantity = 1 }, // Ultimate → Glamping Tipi
            new PackageItem { PackageId = 3, ItemId = 13, Quantity = 1 }, // Ultimate → VIP Lounge
            new PackageItem { PackageId = 3, ItemId = 5,  Quantity = 2 }, // Ultimate → 2x Drink Pack
            new PackageItem { PackageId = 6, ItemId = 9,  Quantity = 1 }, // VIP All-In → VIP Parking
            new PackageItem { PackageId = 6, ItemId = 14, Quantity = 1 }, // VIP All-In → Meet & Greet
            new PackageItem { PackageId = 6, ItemId = 4,  Quantity = 3 }  // VIP All-In → 3x Meal Voucher
        );
    }
}
```

---

## Repository Implementations

Create folder `FestivalTickets.Infrastructure/Repositories/`

### `BookingRepository.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;
    public BookingRepository(ApplicationDbContext db) => _db = db;

    public async Task<Booking?> GetByIdAsync(int id) =>
        await _db.Bookings
            .Include(b => b.Package).ThenInclude(p => p!.Festival)
            .Include(b => b.Package).ThenInclude(p => p!.PackageItems).ThenInclude(pi => pi.Item)
            .Include(b => b.Customer)
            .Include(b => b.BookingItems).ThenInclude(bi => bi.Item)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<Booking>> GetAllAsync() =>
        await _db.Bookings
            .Include(b => b.Package).ThenInclude(p => p!.Festival)
            .Include(b => b.Customer)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId) =>
        await _db.Bookings
            .Include(b => b.Package).ThenInclude(p => p!.Festival)
            .Include(b => b.BookingItems).ThenInclude(bi => bi.Item)
            .Where(b => b.CustId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

    public async Task AddAsync(Booking booking)
    {
        await _db.Bookings.AddAsync(booking);
    }

    public async Task DeleteAsync(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingItems)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return;

        _db.BookingItems.RemoveRange(booking.BookingItems);
        _db.Bookings.Remove(booking);
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
```

---

### `CustomerRepository.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _db;
    public CustomerRepository(ApplicationDbContext db) => _db = db;

    public async Task<Customer?> GetByUserIdAsync(string userId) =>
        await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Customer?> GetByIdAsync(int id) =>
        await _db.Customers.FindAsync(id);

    public async Task<IEnumerable<Customer>> GetAllAsync() =>
        await _db.Customers.ToListAsync();

    public async Task AddAsync(Customer customer) =>
        await _db.Customers.AddAsync(customer);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
```

---

### `FestivalRepository.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class FestivalRepository : IFestivalRepository
{
    private readonly ApplicationDbContext _db;
    public FestivalRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Festival>> GetAllAsync() =>
        await _db.Festivals.ToListAsync();

    public async Task<IEnumerable<Festival>> GetUpcomingAsync(DateOnly from, DateOnly to) =>
        await _db.Festivals
            .Where(f => f.StartDate >= from && f.StartDate <= to)
            .OrderBy(f => f.StartDate)
            .ToListAsync();

    public async Task<Festival?> GetByIdWithPackagesAsync(int id) =>
        await _db.Festivals
            .Include(f => f.Packages)
                .ThenInclude(p => p.PackageItems)
                    .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(f => f.Id == id);
}
```

---

### `PackageRepository.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class PackageRepository : IPackageRepository
{
    private readonly ApplicationDbContext _db;
    public PackageRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Package>> GetByFestivalIdAsync(int festivalId) =>
        await _db.Packages
            .Where(p => p.FestivalId == festivalId)
            .Include(p => p.PackageItems).ThenInclude(pi => pi.Item)
            .ToListAsync();

    public async Task<Package?> GetByIdWithItemsAsync(int id) =>
        await _db.Packages
            .Include(p => p.PackageItems).ThenInclude(pi => pi.Item)
            .Include(p => p.Festival)
            .FirstOrDefaultAsync(p => p.Id == id);
}
```

---

### `ItemRepository.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FestivalTickets.Infrastructure.Repositories;

public sealed class ItemRepository : IItemRepository
{
    private readonly ApplicationDbContext _db;
    public ItemRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Item>> GetAllAsync() =>
        await _db.Items.OrderBy(i => i.ItemType).ThenBy(i => i.Name).ToListAsync();
}
```

---

## Migrations

After writing all of the above, run:

```bash
dotnet ef migrations add AddIdentityAndBookings \
  -p FestivalTickets.Infrastructure \
  -s FestivalTickets.Web

dotnet ef database update \
  -p FestivalTickets.Infrastructure \
  -s FestivalTickets.Web
```

---

## Identity Seed (User accounts + roles)

Because password hashing happens at runtime, seed users/roles in `Program.cs` using a startup service — NOT via `HasData()`. See Phase 3 for the full `Program.cs` including this seeder.

Seed accounts:
| Role          | Email                       | Password        | Loyalty Card |
|---------------|-----------------------------|-----------------|--------------|
| Administrator | admin@festivaltickets.nl    | Admin@123!      | —            |
| Customer      | lisa@festivaltickets.nl     | Customer@123!   | YES          |
| Customer      | tom@festivaltickets.nl      | Customer@123!   | NO           |

---

---

# PHASE 3 — Web Layer: Program.cs, Identity Setup, Account Controller

## Goal
Wire up Identity, Session, DI registrations. Build Login/Register pages. Redirect roles appropriately.

## `FestivalTickets.Web.csproj` — add packages

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="9.*" />
```

Also add project reference to Infrastructure (should already be there).

---

## `Program.cs` — FULL FILE

```csharp
using FestivalTickets.Domain.Discounts;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Infrastructure;
using FestivalTickets.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + Session ────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── Database ──────────────────────────────────────────────────────────
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(cs, sql => sql.MigrationsAssembly("FestivalTickets.Infrastructure")));

// ── Identity ──────────────────────────────────────────────────────────
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength         = 6;
    options.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── Cookie paths ──────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath    = "/Account/Login";
    options.LogoutPath   = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ── Repository DI ─────────────────────────────────────────────────────
builder.Services.AddScoped<IBookingRepository,  BookingRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IFestivalRepository, FestivalRepository>();
builder.Services.AddScoped<IPackageRepository,  PackageRepository>();
builder.Services.AddScoped<IItemRepository,     ItemRepository>();

// ── Discount Calculator (transient — new instance each time) ──────────
builder.Services.AddTransient<DiscountCalculator>();

var app = builder.Build();

// ── Dutch locale ──────────────────────────────────────────────────────
var cultureInfo = new CultureInfo("nl-NL");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureInfo),
    SupportedCultures    = new List<CultureInfo> { cultureInfo },
    SupportedUICultures  = new List<CultureInfo> { cultureInfo }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // ← MUST come before UseAuthorization
app.UseAuthorization();
app.UseSession();        // ← MUST come after UseAuthorization, before MapControllerRoute

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── Seed Identity roles + users ───────────────────────────────────────
await SeedIdentityAsync(app);

app.Run();

// ─────────────────────────────────────────────────────────────────────
static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure roles exist
    foreach (var role in new[] { "Administrator", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Admin account
    await EnsureUserAsync(userManager, db,
        email: "admin@festivaltickets.nl",
        password: "Admin@123!",
        role: "Administrator",
        firstName: "Admin",
        lastName: "User",
        loyaltyCard: false);

    // Customer with loyalty card
    await EnsureUserAsync(userManager, db,
        email: "lisa@festivaltickets.nl",
        password: "Customer@123!",
        role: "Customer",
        firstName: "Lisa",
        lastName: "Janssen",
        loyaltyCard: true);

    // Customer without loyalty card
    await EnsureUserAsync(userManager, db,
        email: "tom@festivaltickets.nl",
        password: "Customer@123!",
        role: "Customer",
        firstName: "Tom",
        lastName: "de Vries",
        loyaltyCard: false);
}

static async Task EnsureUserAsync(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext db,
    string email, string password, string role,
    string firstName, string lastName, bool loyaltyCard)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new Exception($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    if (!await userManager.IsInRoleAsync(user, role))
        await userManager.AddToRoleAsync(user, role);

    if (loyaltyCard)
    {
        var claims = await userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == "LoyaltyCard"))
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("LoyaltyCard", "true"));
    }

    // Create Customer record (for Customer role only)
    if (role == "Customer" && !db.Customers.Any(c => c.UserId == user.Id))
    {
        db.Customers.Add(new FestivalTickets.Domain.Customer
        {
            FirstName = firstName,
            LastName  = lastName,
            Email     = email,
            UserId    = user.Id
        });
        await db.SaveChangesAsync();
    }
}
```

---

## `Controllers/AccountController.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser>  _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ICustomerRepository _customerRepo;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ICustomerRepository customerRepo)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _customerRepo  = customerRepo;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST /Account/Login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            // Redirect Administrator to bookings, Customer to welcome
            if (user != null && await _userManager.IsInRoleAsync(user, "Administrator"))
                return RedirectToAction("Index", "AdminBookings");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Ongeldig e-mailadres of wachtwoord.");
        return View(model);
    }

    // GET /Account/Register
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    // POST /Account/Register
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");

            var customer = new Customer
            {
                FirstName = model.FirstName,
                LastName  = model.LastName,
                Email     = model.Email,
                UserId    = user.Id
            };
            await _customerRepo.AddAsync(customer);
            await _customerRepo.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // POST /Account/Logout
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/AccessDenied
    public IActionResult AccessDenied() => View();

    // GET /Account/Profile
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        return View(new ProfileViewModel
        {
            FirstName = customer.FirstName,
            LastName  = customer.LastName,
            Email     = user.Email ?? string.Empty
        });
    }

    // POST /Account/Profile
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        customer.FirstName = model.FirstName;
        customer.LastName  = model.LastName;
        await _customerRepo.SaveChangesAsync();

        TempData["Success"] = "Profiel bijgewerkt.";
        return RedirectToAction(nameof(Profile));
    }
}
```

---

## ViewModels for Account (in `Models/`)

### `LoginViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Onthoud mij")]
    public bool RememberMe { get; set; }
}
```

### `RegisterViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public class RegisterViewModel
{
    [Required, MaxLength(100)]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Achternaam")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    [Display(Name = "Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Wachtwoorden komen niet overeen.")]
    [Display(Name = "Bevestig wachtwoord")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### `ProfileViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public class ProfileViewModel
{
    [Required, MaxLength(100)]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Achternaam")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty; // read-only display
}
```

---

## Views for Account

### `Views/Account/Login.cshtml`
Standard Bootstrap form posting to `Account/Login`. Fields: Email, Password, RememberMe checkbox. Link to Register page.

### `Views/Account/Register.cshtml`
Bootstrap form posting to `Account/Register`. Fields: FirstName, LastName, Email, Password, ConfirmPassword. Link to Login page.

### `Views/Account/Profile.cshtml`
Form showing FirstName, LastName (editable), Email (read-only label). Save button. `[Authorize(Roles="Customer")]`.

### `Views/Account/AccessDenied.cshtml`
Simple message: "U heeft geen toegang tot deze pagina."

---

## Update `_Layout.cshtml`

Inject `UserManager` and `SignInManager` via `@inject`. Show logged-in user name and role in the navbar. Show Logout button. Show role-based nav links:
- Anonymous: Login, Register
- Customer: MyBookings, Profile, Logout
- Administrator: All Bookings, Festivals, Packages, Items, Logout

Partial for user info: `@await Html.RenderPartialAsync("_LoginPartial")`

Create `Views/Shared/_LoginPartial.cshtml` with the full conditional nav links. Inject `SignInManager<IdentityUser>` and `UserManager<IdentityUser>`.

---

---

# PHASE 4 — Administrator: Bookings & Loyalty Card

## Goal
Administrator-only views: list all bookings, view/delete a booking, grant/revoke loyalty card claims.

## `Controllers/AdminBookingsController.cs`

```csharp
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminBookingsController : Controller
{
    private readonly IBookingRepository  _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminBookingsController(
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        UserManager<IdentityUser> userManager)
    {
        _bookingRepo  = bookingRepo;
        _customerRepo = customerRepo;
        _userManager  = userManager;
    }

    // GET: /AdminBookings/Index — list all bookings
    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingRepo.GetAllAsync();
        return View(bookings);
    }

    // GET: /AdminBookings/Details/5 — invoice view for one booking
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingRepo.GetByIdAsync(id);
        if (booking == null) return NotFound();
        return View(booking);
    }

    // GET: /AdminBookings/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _bookingRepo.GetByIdAsync(id);
        if (booking == null) return NotFound();
        return View(booking);
    }

    // POST: /AdminBookings/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _bookingRepo.DeleteAsync(id);
        await _bookingRepo.SaveChangesAsync();
        TempData["Success"] = "Boeking verwijderd.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /AdminBookings/ManageLoyalty — list customers with loyalty card status
    public async Task<IActionResult> ManageLoyalty()
    {
        var customers = await _customerRepo.GetAllAsync();
        var vms = new List<LoyaltyViewModel>();

        foreach (var customer in customers)
        {
            var user = await _userManager.FindByIdAsync(customer.UserId);
            if (user == null) continue;
            var claims = await _userManager.GetClaimsAsync(user);
            vms.Add(new LoyaltyViewModel
            {
                CustomerId = customer.Id,
                FullName   = $"{customer.FirstName} {customer.LastName}",
                Email      = customer.Email,
                HasLoyaltyCard = claims.Any(c => c.Type == "LoyaltyCard" && c.Value == "true")
            });
        }

        return View(vms);
    }

    // POST: /AdminBookings/GrantLoyalty/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantLoyalty(int customerId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null) return NotFound();

        var user = await _userManager.FindByIdAsync(customer.UserId);
        if (user == null) return NotFound();

        var claims = await _userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == "LoyaltyCard"))
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("LoyaltyCard", "true"));

        TempData["Success"] = $"Loyaliteitskaart toegekend aan {customer.FirstName}.";
        return RedirectToAction(nameof(ManageLoyalty));
    }

    // POST: /AdminBookings/RevokeLoyalty/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeLoyalty(int customerId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null) return NotFound();

        var user = await _userManager.FindByIdAsync(customer.UserId);
        if (user == null) return NotFound();

        var claims = await _userManager.GetClaimsAsync(user);
        var loyaltyClaim = claims.FirstOrDefault(c => c.Type == "LoyaltyCard");
        if (loyaltyClaim != null)
            await _userManager.RemoveClaimAsync(user, loyaltyClaim);

        TempData["Success"] = $"Loyaliteitskaart ingetrokken van {customer.FirstName}.";
        return RedirectToAction(nameof(ManageLoyalty));
    }
}
```

---

## ViewModels

### `LoyaltyViewModel.cs`
```csharp
namespace FestivalTickets.Web.Models;

public class LoyaltyViewModel
{
    public int    CustomerId    { get; set; }
    public string FullName      { get; set; } = string.Empty;
    public string Email         { get; set; } = string.Empty;
    public bool   HasLoyaltyCard { get; set; }
}
```

---

## Views (in `Views/AdminBookings/`)

### `Index.cshtml`
Table of all bookings: columns = BookingDate, CustomerName, FestivalName, PackageName, Quantity, TotalPricePaid, Actions (Details | Delete).
Show `TempData["Success"]` at top.

### `Details.cshtml`
Full invoice: festival info, package name, ticket quantity, list of extra BookingItems (name, qty, subtotal), all discount lines, and final total. Read-only.

### `Delete.cshtml`
Confirmation page showing booking summary. POST button to confirm.

### `ManageLoyalty.cshtml`
Table of all customers: columns = FullName, Email, LoyaltyCard status (badge), Actions.
- If `HasLoyaltyCard`: show "Intrekken" POST button.
- If not: show "Toekennen" POST button.

---

---

# PHASE 5 — Customer: MyBookings

## Goal
Customer can see their own booking history and view a booking invoice.

## `Controllers/CustomerBookingsController.cs`

```csharp
using FestivalTickets.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerBookingsController : Controller
{
    private readonly IBookingRepository  _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly UserManager<IdentityUser> _userManager;

    public CustomerBookingsController(
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        UserManager<IdentityUser> userManager)
    {
        _bookingRepo  = bookingRepo;
        _customerRepo = customerRepo;
        _userManager  = userManager;
    }

    // GET: /CustomerBookings/Index — MyBookings
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        var bookings = await _bookingRepo.GetByCustomerIdAsync(customer.Id);
        return View(bookings);
    }

    // GET: /CustomerBookings/Details/5 — invoice
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        var booking = await _bookingRepo.GetByIdAsync(id);
        if (booking == null || booking.CustId != customer.Id)
            return Forbid(); // customer can only see own bookings

        return View(booking);
    }
}
```

## Views (in `Views/CustomerBookings/`)

### `Index.cshtml`
Table: BookingDate, FestivalName, PackageName, Quantity, TotalPricePaid, link to Details.
Heading: "Mijn Boekingen".

### `Details.cshtml`
Same invoice layout as `AdminBookings/Details.cshtml`. Can be a shared partial `_BookingInvoice.cshtml` to avoid duplication.

---

---

# PHASE 6 — Booking Wizard (5 Steps)

## Goal
Build the core feature: a 5-step booking process using `HttpContext.Session`.

## Session State Design

Create `Models/BookingWizardState.cs`:

```csharp
namespace FestivalTickets.Web.Models;

/// <summary>
/// Stored in HttpContext.Session as JSON. Persists across wizard steps.
/// Reset on Cancel or after successful confirmation.
/// </summary>
public class BookingWizardState
{
    // Step 1
    public int?     SelectedFestivalId { get; set; }
    public DateOnly FilterFrom         { get; set; }
    public DateOnly FilterTo           { get; set; }

    // Step 2
    public int? SelectedPackageId   { get; set; }
    public int  TicketQuantity      { get; set; } = 1;

    // Step 3: list of extra items chosen by user (ItemId → quantity)
    public List<WizardItemEntry> ExtraItems { get; set; } = new();

    // Step 5: computed values stored before confirmation
    public decimal BaseTotal         { get; set; }
    public decimal FinalTotal        { get; set; }
    public List<DiscountLine> Discounts { get; set; } = new();
}

public class WizardItemEntry
{
    public int     ItemId   { get; set; }
    public string  ItemName { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public decimal UnitPrice { get; set; }
    public int     Quantity  { get; set; }
}

public class DiscountLine
{
    public string  Name   { get; set; } = string.Empty;
    public decimal Saving { get; set; }
}
```

---

## Session Helper (static class in `Models/`)

```csharp
using System.Text.Json;

namespace FestivalTickets.Web.Models;

public static class WizardSessionHelper
{
    private const string Key = "BookingWizard";

    public static BookingWizardState Load(ISession session)
    {
        var json = session.GetString(Key);
        if (string.IsNullOrEmpty(json)) return new BookingWizardState();
        return JsonSerializer.Deserialize<BookingWizardState>(json) ?? new BookingWizardState();
    }

    public static void Save(ISession session, BookingWizardState state)
    {
        session.SetString(Key, JsonSerializer.Serialize(state));
    }

    public static void Clear(ISession session) => session.Remove(Key);
}
```

---

## `Controllers/BookingWizardController.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Discounts;
using FestivalTickets.Domain.Interfaces;
using FestivalTickets.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTickets.Web.Controllers;

public class BookingWizardController : Controller
{
    private readonly IFestivalRepository _festivalRepo;
    private readonly IPackageRepository  _packageRepo;
    private readonly IItemRepository     _itemRepo;
    private readonly IBookingRepository  _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly DiscountCalculator  _discountCalc;

    public BookingWizardController(
        IFestivalRepository festivalRepo,
        IPackageRepository packageRepo,
        IItemRepository itemRepo,
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        UserManager<IdentityUser> userManager,
        DiscountCalculator discountCalc)
    {
        _festivalRepo = festivalRepo;
        _packageRepo  = packageRepo;
        _itemRepo     = itemRepo;
        _bookingRepo  = bookingRepo;
        _customerRepo = customerRepo;
        _userManager  = userManager;
        _discountCalc = discountCalc;
    }

    // ── STEP 1: Choose Festival ─────────────────────────────────────────

    // GET /BookingWizard/Step1
    public async Task<IActionResult> Step1(DateOnly? from, DateOnly? to)
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);

        // Default filter: today to +2 years
        var today = DateOnly.FromDateTime(DateTime.Today);
        state.FilterFrom = from ?? state.FilterFrom == default ? today : state.FilterFrom;
        state.FilterTo   = to   ?? state.FilterTo   == default ? today.AddYears(2) : state.FilterTo;
        WizardSessionHelper.Save(HttpContext.Session, state);

        var festivals = await _festivalRepo.GetUpcomingAsync(state.FilterFrom, state.FilterTo);
        var vm = new Step1ViewModel
        {
            Festivals         = festivals.ToList(),
            FilterFrom        = state.FilterFrom,
            FilterTo          = state.FilterTo,
            SelectedFestivalId = state.SelectedFestivalId
        };
        return View(vm);
    }

    // POST /BookingWizard/Step1
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Step1(Step1ViewModel model)
    {
        if (model.SelectedFestivalId == null)
        {
            ModelState.AddModelError("", "Selecteer een festival.");
            return View(model);
        }
        var state = WizardSessionHelper.Load(HttpContext.Session);
        state.SelectedFestivalId = model.SelectedFestivalId;
        // Reset downstream selections when festival changes
        state.SelectedPackageId = null;
        state.ExtraItems.Clear();
        WizardSessionHelper.Save(HttpContext.Session, state);
        return RedirectToAction(nameof(Step2));
    }

    // ── STEP 2: Choose Package/Ticket ───────────────────────────────────

    // GET /BookingWizard/Step2
    public async Task<IActionResult> Step2()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedFestivalId == null)
            return RedirectToAction(nameof(Step1));

        var festival = await _festivalRepo.GetByIdWithPackagesAsync(state.SelectedFestivalId.Value);
        if (festival == null) return NotFound();

        var vm = new Step2ViewModel
        {
            Festival           = festival,
            Packages           = festival.Packages.ToList(),
            SelectedPackageId  = state.SelectedPackageId,
            TicketQuantity     = state.TicketQuantity
        };
        return View(vm);
    }

    // POST /BookingWizard/Step2
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Step2(Step2ViewModel model)
    {
        if (model.SelectedPackageId == null || model.TicketQuantity < 1)
        {
            ModelState.AddModelError("", "Selecteer een ticket en voer een geldig aantal in.");
            return View(model);
        }
        var state = WizardSessionHelper.Load(HttpContext.Session);
        state.SelectedPackageId = model.SelectedPackageId;
        state.TicketQuantity    = model.TicketQuantity;
        WizardSessionHelper.Save(HttpContext.Session, state);
        return RedirectToAction(nameof(Step3));
    }

    // ── STEP 3: Extra Items ─────────────────────────────────────────────

    // GET /BookingWizard/Step3
    public async Task<IActionResult> Step3()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
            return RedirectToAction(nameof(Step2));

        var allItems = await _itemRepo.GetAllAsync();
        var vm = new Step3ViewModel
        {
            AllItems   = allItems.ToList(),
            ExtraItems = state.ExtraItems
        };
        return View(vm);
    }

    // POST /BookingWizard/Step3
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Step3(Step3ViewModel model)
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        var allItems = (await _itemRepo.GetAllAsync()).ToDictionary(i => i.Id);

        // Build the extra items list from the submitted form
        state.ExtraItems.Clear();
        if (model.SelectedItemIds != null)
        {
            for (int i = 0; i < model.SelectedItemIds.Count; i++)
            {
                var itemId = model.SelectedItemIds[i];
                var qty    = model.Quantities != null && i < model.Quantities.Count ? model.Quantities[i] : 1;

                if (qty < 1 || !allItems.TryGetValue(itemId, out var item)) continue;

                state.ExtraItems.Add(new WizardItemEntry
                {
                    ItemId    = itemId,
                    ItemName  = item.Name,
                    ItemType  = item.ItemType,
                    UnitPrice = item.Price,
                    Quantity  = qty
                });
            }
        }

        WizardSessionHelper.Save(HttpContext.Session, state);
        return RedirectToAction(nameof(Step4));
    }

    // ── STEP 4: Authentication ──────────────────────────────────────────

    // GET /BookingWizard/Step4
    public IActionResult Step4()
    {
        // If already logged in as Customer, skip to Step 5
        if (User.IsInRole("Customer"))
            return RedirectToAction(nameof(Step5));

        // If logged in as Administrator, they cannot book
        if (User.IsInRole("Administrator"))
        {
            TempData["Error"] = "Beheerders kunnen geen tickets boeken.";
            return RedirectToAction("Index", "AdminBookings");
        }

        return View(); // Show login/register options
    }

    // ── STEP 5: Confirm / Invoice ───────────────────────────────────────

    // GET /BookingWizard/Step5
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Step5()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
            return RedirectToAction(nameof(Step1));

        var package = await _packageRepo.GetByIdWithItemsAsync(state.SelectedPackageId.Value);
        if (package == null) return NotFound();

        // Build discount context
        bool hasLoyalty = User.HasClaim("LoyaltyCard", "true");

        // Base total = (festival BasicPrice × ticketQty) + package items cost × ticketQty + extra items
        decimal packageItemsCost = package.PackageItems.Sum(pi => pi.Item.Price * pi.Quantity);
        decimal baseTotal = (package.Festival!.BasicPrice + packageItemsCost) * state.TicketQuantity
                          + state.ExtraItems.Sum(e => e.UnitPrice * e.Quantity);

        var context = new DiscountContext
        {
            BaseTotal        = baseTotal,
            CurrentTotal     = baseTotal,
            TicketQuantity   = state.TicketQuantity,
            BookingDate      = DateTime.Now,
            FestivalStartDate = package.Festival!.StartDate,
            HasLoyaltyCard   = hasLoyalty,
            ExtraItems       = state.ExtraItems.Select(e =>
                (e.ItemId, e.ItemType, e.UnitPrice, e.Quantity)).ToList()
        };

        var breakdown = _discountCalc.GetBreakdown(context);

        // Re-calculate fresh final total
        context.CurrentTotal = context.BaseTotal;
        decimal finalTotal = _discountCalc.Calculate(context);

        // Store computed values in session for the POST
        state.BaseTotal  = baseTotal;
        state.FinalTotal = finalTotal;
        state.Discounts  = breakdown.Select(d => new DiscountLine { Name = d.Name, Saving = d.Saving }).ToList();
        WizardSessionHelper.Save(HttpContext.Session, state);

        var vm = new Step5ViewModel
        {
            Package        = package,
            TicketQuantity = state.TicketQuantity,
            ExtraItems     = state.ExtraItems,
            BaseTotal      = baseTotal,
            DiscountLines  = state.Discounts,
            FinalTotal     = finalTotal
        };
        return View(vm);
    }

    // POST /BookingWizard/Confirm — writes booking to DB
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Confirm()
    {
        var state = WizardSessionHelper.Load(HttpContext.Session);
        if (state.SelectedPackageId == null)
            return RedirectToAction(nameof(Step1));

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var customer = await _customerRepo.GetByUserIdAsync(user.Id);
        if (customer == null) return NotFound();

        var booking = new Booking
        {
            PackageId      = state.SelectedPackageId.Value,
            Quantity       = state.TicketQuantity,
            CustId         = customer.Id,
            BookingDate    = DateTime.Now,
            TotalPricePaid = state.FinalTotal,
            BookingItems   = state.ExtraItems.Select(e => new BookingItem
            {
                ItemId   = e.ItemId,
                Quantity = e.Quantity
            }).ToList()
        };

        await _bookingRepo.AddAsync(booking);
        await _bookingRepo.SaveChangesAsync();

        WizardSessionHelper.Clear(HttpContext.Session);
        TempData["Success"] = "Boeking bevestigd!";
        return RedirectToAction("Details", "CustomerBookings", new { id = booking.Id });
    }

    // POST /BookingWizard/Cancel
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        WizardSessionHelper.Clear(HttpContext.Session);
        return RedirectToAction("Index", "Home");
    }
}
```

---

## ViewModels for Wizard (in `Models/`)

### `Step1ViewModel.cs`
```csharp
using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

public class Step1ViewModel
{
    public List<Festival> Festivals          { get; set; } = new();
    public int?           SelectedFestivalId { get; set; }
    public DateOnly       FilterFrom         { get; set; }
    public DateOnly       FilterTo           { get; set; }
}
```

### `Step2ViewModel.cs`
```csharp
using FestivalTickets.Domain;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public class Step2ViewModel
{
    public Festival?      Festival          { get; set; }
    public List<Package>  Packages          { get; set; } = new();
    public int?           SelectedPackageId { get; set; }

    [Range(1, 1000, ErrorMessage = "Voer minimaal 1 ticket in.")]
    [Display(Name = "Aantal tickets")]
    public int TicketQuantity { get; set; } = 1;
}
```

### `Step3ViewModel.cs`
```csharp
using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

public class Step3ViewModel
{
    public List<Item>            AllItems        { get; set; } = new();
    public List<WizardItemEntry> ExtraItems      { get; set; } = new();
    // Submitted form values (parallel lists)
    public List<int>?            SelectedItemIds { get; set; }
    public List<int>?            Quantities      { get; set; }
}
```

### `Step5ViewModel.cs`
```csharp
using FestivalTickets.Domain;

namespace FestivalTickets.Web.Models;

public class Step5ViewModel
{
    public Package?              Package        { get; set; }
    public int                   TicketQuantity { get; set; }
    public List<WizardItemEntry> ExtraItems     { get; set; } = new();
    public decimal               BaseTotal      { get; set; }
    public List<DiscountLine>    DiscountLines  { get; set; } = new();
    public decimal               FinalTotal     { get; set; }
}
```

---

## Views for Wizard (in `Views/BookingWizard/`)

**All wizard views** must show a step indicator at the top: Step 1 | Step 2 | Step 3 | Step 4 | Step 5. Highlight the current step.
All views must have a **[Cancel]** button (POST to `/BookingWizard/Cancel`).
All views from Step 2 onwards must have a **[Back]** button (link to previous step GET).

### `Step1.cshtml`
- Date filter form (FilterFrom, FilterTo) with a [Filter] submit button — navigates back to Step1 GET with query params.
- List of festivals (cards or table): Name, Place, StartDate–EndDate, BasicPrice, Logo, "Kies" radio or button.
- POST to Step1.

### `Step2.cshtml`
- Show chosen festival info at top.
- List of packages for that festival: Name, total price (BasicPrice + items), list of included items.
- Radio buttons to select a package.
- Number input for TicketQuantity (min=1, default=1).
- POST to Step2.

### `Step3.cshtml`
- Show current selection summary (festival + package + qty) at top.
- Table of all items grouped by ItemType. For each: checkbox/add button, quantity input.
- Items already selected shown with current quantity.
- POST to Step3.

### `Step4.cshtml`
- Message: "Je moet ingelogd zijn om verder te gaan."
- Two columns: Login form | Register form.
- After login/register, redirect to Step5.
- **Important**: the login/register on this page must pass `returnUrl=/BookingWizard/Step5` to the Account controller.

### `Step5.cshtml` (Invoice + Confirmation)
- Full invoice layout:
  - Festival name, Package name, ticket qty × ticket price = subtotal
  - Extra items table: item name, qty, unit price, line total
  - Separator line: subtotal
  - Per discount: name, -€saving
  - **Total: €finalTotal** (bold)
- [Bevestig & Betaal] POST to `/BookingWizard/Confirm`
- [Terug] link to Step3

---

---

# PHASE 7 — Update Home View & Welcome Screen

## `Views/Home/Index.cshtml`

Replace with a proper welcome screen that:
- Shows the app name and a hero description.
- If **anonymous**: shows a "Book a Ticket" button linking to `/BookingWizard/Step1` and a "Login" / "Register" link.
- If **Customer**: shows a "Book a Ticket" button + "My Bookings" link.
- If **Administrator**: shows message "Welcome, Administrator" and links to Bookings, Festivals, Packages, Items.
- Never redirects automatically — the welcome screen is always shown to everyone.

---

---

# PHASE 8 — Unit Tests

## Goal
Build `FestivalTickets.Tests` with xUnit, Moq, InMemory repository. Achieve >75% coverage of business logic.

## Test file structure

```
FestivalTickets.Tests/
├── Discounts/
│   ├── TShirtDiscountStrategyTests.cs
│   ├── EarlyBirdDiscountStrategyTests.cs
│   ├── GroupDiscountStrategyTests.cs
│   ├── LoyaltyCardDiscountStrategyTests.cs
│   └── DiscountCalculatorTests.cs
├── Repositories/
│   ├── InMemoryBookingRepository.cs
│   ├── InMemoryCustomerRepository.cs
│   └── BookingRepositoryTests.cs
└── Services/
    └── PriceCalculationTests.cs
```

---

## `InMemoryBookingRepository.cs`

```csharp
using FestivalTickets.Domain;
using FestivalTickets.Domain.Interfaces;

namespace FestivalTickets.Tests.Repositories;

/// <summary>
/// In-memory implementation of IBookingRepository for tests.
/// No database needed.
/// </summary>
public class InMemoryBookingRepository : IBookingRepository
{
    private readonly List<Booking> _store = new();

    public Task<Booking?> GetByIdAsync(int id) =>
        Task.FromResult(_store.FirstOrDefault(b => b.Id == id));

    public Task<IEnumerable<Booking>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Booking>>(_store.ToList());

    public Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId) =>
        Task.FromResult<IEnumerable<Booking>>(_store.Where(b => b.CustId == customerId).ToList());

    public Task AddAsync(Booking booking)
    {
        booking.Id = _store.Count + 1;
        _store.Add(booking);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var b = _store.FirstOrDefault(x => x.Id == id);
        if (b != null) _store.Remove(b);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}
```

---

## Discount Tests (write all of the following)

### `TShirtDiscountStrategyTests.cs`

Write tests for:
- **0 t-shirts**: no discount applied.
- **3 t-shirts**: no discount (below threshold).
- **4 t-shirts of same price €20**: €20 discount (1 free).
- **8 t-shirts**: 2 free.
- **4 t-shirts of different prices (€10, €15, €20, €25)**: cheapest one (€10) is free.
- **Mixed: 2 t-shirts + 1 hoodie = 3 Merchandise**: no discount yet (only 3 not 4).
- **4 t-shirts + 1 parking item**: t-shirt discount applies (parking is not Merchandise).

### `EarlyBirdDiscountStrategyTests.cs`

Write tests for:
- **BookingDate exactly 5 months before festival**: 15% discount.
- **BookingDate 6 months before**: 15% discount.
- **BookingDate 4 months before**: no discount.
- **BookingDate day before festival**: no discount.
- **Discount only on ticket portion, not extra items**: verify calculation separates ticket vs items.

### `GroupDiscountStrategyTests.cs`

Write tests for:
- **4 tickets**: no discount.
- **5 tickets**: 20% off ticket portion.
- **10 tickets**: 20% off ticket portion.

### `LoyaltyCardDiscountStrategyTests.cs`

Write tests for:
- **HasLoyaltyCard = false**: no discount.
- **HasLoyaltyCard = true**: 10% off total.
- **Applied after other discounts**: verify it multiplies the already-discounted total.

### `DiscountCalculatorTests.cs`

Write tests for:
- **No discounts apply**: final = base.
- **Multiple discounts stack**: T-shirt + loyalty card together.
- **Injecting mock strategies**: use Moq to inject a strategy that always returns 50% off, verify calculator uses it.
- **Price never goes negative**: if discount exceeds base, result is €0.

### `BookingRepositoryTests.cs`

Write tests using `InMemoryBookingRepository`:
- **Add and retrieve booking by id**: happy path.
- **GetByCustomerId returns only that customer's bookings**.
- **Delete removes booking**.

### `PriceCalculationTests.cs`

Integration-style test: given a package with known items and extra items, verify the full price calculation pipeline (base → discounts → final total) produces the expected number.

---

## Code coverage

Run: `dotnet test --collect:"XPlat Code Coverage"`
Target: all discount strategy classes + `DiscountCalculator` fully covered.

---

---

# PHASE 9 — Final Polish & Validation

## Validation checklist

Go through every form in the app and verify:
- All required fields have `[Required]` on the model/viewmodel.
- All numeric fields have `[Range]` with sensible min/max.
- All string fields have `[MaxLength]`.
- Every POST action checks `if (!ModelState.IsValid)` and returns the view with errors.
- Client-side validation scripts (`_ValidationScriptsPartial`) are included in every view with a form.

## Authorization checklist

| Controller / Action                    | Required Role      |
|----------------------------------------|--------------------|
| AdminBookingsController (all)          | Administrator      |
| CustomerBookingsController (all)       | Customer           |
| BookingWizardController.Step5 (GET/POST) | Customer         |
| BookingWizardController.Confirm (POST) | Customer           |
| AccountController.Profile              | Customer           |
| FestivalsController (all)              | Administrator      |
| PackagesController (all)               | Administrator      |
| ItemsController (all)                  | Administrator      |
| HomeController.Index                   | Anonymous (all)    |
| BookingWizard Step1–Step3              | Anonymous (all)    |
| BookingWizard Step4                    | Anonymous (redirects to login) |

Apply `[Authorize(Roles = "Administrator")]` to `FestivalsController`, `PackagesController`, `ItemsController` — PROG5 controllers. Customers and anonymous users should not be able to manage festival config.

## UI checklist

- Logged-in user name (and role) always visible in navbar.
- `TempData["Success"]` and `TempData["Error"]` shown in a Bootstrap alert on every relevant view.
- All currency amounts formatted as `€ X.XX` using `@Model.X.ToString("C")` with nl-NL culture.
- All dates formatted as `dd-MM-yyyy`.
- Every list view shows "Geen gegevens beschikbaar." when empty.
- Responsive Bootstrap layout on all pages.
- Step indicator on all wizard pages (e.g. breadcrumb: Step 1 ✓ → **Step 2** → Step 3 → Step 4 → Step 5).

## Database checklist

- Run `dotnet ef database update` fresh from scratch. App starts, welcome page shows. No migration errors.
- Seeded accounts can log in with the passwords listed in Phase 2.
- Administrator sees booking list on login.
- Customer (lisa) sees loyalty card discount applied in wizard.

---

---

# Summary: Build Order

| Phase | What you build                                         | Time est. |
|-------|--------------------------------------------------------|-----------|
| 0     | Rename PROG5 → FestivalTickets, add Test project       | 30 min    |
| 1     | Domain: Customer, Booking, BookingItem, interfaces, discounts | 60 min |
| 2     | Infrastructure: DbContext extend, repositories, migrations, seed | 60 min |
| 3     | Web: Program.cs, Identity, Account controller + views  | 45 min    |
| 4     | Admin: BookingsController + views, Loyalty card        | 45 min    |
| 5     | Customer: MyBookings controller + views                | 30 min    |
| 6     | Booking Wizard (5 steps, session, price calc)          | 90 min    |
| 7     | Welcome page update + navbar role-based links          | 20 min    |
| 8     | Unit Tests (all strategy tests + in-memory repo tests) | 60 min    |
| 9     | Final polish: validation, authorization, UI, test run  | 30 min    |

**Total estimated time: ~8 hours of focused agent work.**

---

*End of build plan. Feed one phase at a time to your coding agent.*