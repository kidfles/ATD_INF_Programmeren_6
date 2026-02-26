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
