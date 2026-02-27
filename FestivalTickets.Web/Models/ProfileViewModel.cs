using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public sealed class ProfileViewModel
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Achternaam")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}
