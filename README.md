# FestivalTickets 🎪

Een ASP.NET Core MVC applicatie waarmee klanten festivaltickets kunnen boeken, inclusief kampeerplekken, parking, merchandise en meer. Want waarom zou je zelf in de rij staan?

---

## Wat doet dit ding?

- **Booking Wizard** — Een 5-stappen boekingsproces. Kies een festival, een pakket, extra items, log in (ook al zou je het liever overslaan), en bevestig. Dan staat er iets in de database.  
- **Kortingen** — Strategy Pattern. T-shirtkorting, vroegboekkorting, groepskorting en loyaliteitskaartkorting worden automatisch toegepast. Je hoeft er niks voor te doen, behalve op tijd boeken.  
- **Admin-paneel** — Beheerders kunnen alle boekingen inzien, verwijderen, en loyaliteitskaarten toekennen of intrekken. Veel macht, veel verantwoordelijkheid.  
- **Identiteitsbeheer** — Inloggen, registreren, profiel bijwerken. The usual.

---

## Architectuur (kort)

```
FestivalTickets.sln
├── FestivalTickets.Domain          ← Entiteiten, interfaces, kortingsstrategieën
├── FestivalTickets.Infrastructure  ← EF Core, repositories, migraties
├── FestivalTickets.Web             ← MVC controllers, views, viewmodels
└── FestivalTickets.Tests           ← xUnit tests, in-memory repositories
```

Geen AJAX. Geen React. Gewoon eerlijk MVC met session state. Ouderwets, maar het werkt.

---

## Opstarten

### Vereisten
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (of LocalDB)

### Installeren en draaien

```bash
# Kloon het project
git clone <repo-url>
cd ATD_INF_Programmeren_6

# Pas de connectionstring aan in appsettings.Development.json
# (staat in FestivalTickets.Web/)

# Database aanmaken + migraties uitvoeren
dotnet ef database update -p FestivalTickets.Infrastructure -s FestivalTickets.Web

# Opstarten
dotnet run --project FestivalTickets.Web
```

Navigeer naar `https://localhost:xxxx`. Je ziet een welkomstpagina.

---

## Testaccounts

Na het opstarten worden de volgende accounts automatisch aangemaakt:

| Rol           | E-mail                       | Wachtwoord      | Loyaliteitskaart |
|---------------|------------------------------|-----------------|------------------|
| Administrator | admin@festivaltickets.nl     | Admin@123!      | —                |
| Klant         | lisa@festivaltickets.nl      | Customer@123!   | ✅               |
| Klant         | tom@festivaltickets.nl       | Customer@123!   | ❌               |

> Lisa heeft de loyaliteitskaart. Tom niet. Deal with it.

---

## Tests draaien

```bash
dotnet test FestivalTickets.Tests
```

28 tests, allemaal groen. Mocht dat niet zo zijn: controleer of je de juiste branch hebt.

---

## Kortingsregels

| Korting           | Voorwaarde                            | Percentage |
|-------------------|---------------------------------------|------------|
| T-shirtkorting    | Koop 4 merchandise-items → 1 gratis   | variabel   |
| Vroegboekkorting  | Booking ≥ 5 maanden vóór festival     | 15%        |
| Groepskorting     | 5 of meer tickets                     | 20%        |
| Loyaliteitskaart  | Claim aanwezig op account             | 10%        |

Kortingen worden in volgorde toegepast op het lopende totaal. Je kunt ze allemaal tegelijk krijgen als je er zin in hebt.

---

## Technische keuzes

- **ASP.NET Core 9 MVC** — want dat was de opdracht
- **EF Core 9 Code First** — migrations, seed data, geen cascade deletes
- **ASP.NET Identity** — rollen, claims, loyaliteitskaart als Identity Claim
- **Strategy Pattern** — voor de kortingsberekening
- **Repository Pattern** — `IBookingRepository`, `ICustomerRepository`, etc.
- **Session** — voor de booking wizard state (`"BookingWizard"` key, JSON geserialiseerd)
- **xUnit + Moq** — unit tests voor alle kortingsstrategieën en repositories

---

## Bekende beperkingen

- Geen e-mailbevestiging (zou leuk zijn, maar valt buiten de scope)
- Geen zoekfunctie op de festivallijst
- Logo's moeten PNG zijn (bewuste keuze, niet luiheid)
