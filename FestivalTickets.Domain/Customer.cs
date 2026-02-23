using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Domain;

/// <summary>
/// Vertegenwoordigt een geregistreerde klant. 1-op-1 gekoppeld aan een AspNetUsers identiteitsaccount.
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

    /// <summary>Foreign key naar AspNetUsers.Id (string).</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
