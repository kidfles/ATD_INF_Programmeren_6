using System.ComponentModel.DataAnnotations;

namespace FestivalTickets.Web.Models;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Voornaam is verplicht.")]
    [MaxLength(100)]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Achternaam is verplicht.")]
    [MaxLength(100)]
    [Display(Name = "Achternaam")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail is verplicht.")]
    [EmailAddress(ErrorMessage = "Voer een geldig e-mailadres in.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wachtwoord is verplicht.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Wachtwoord moet minstens 6 tekens bevatten.")]
    [Display(Name = "Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bevestig het wachtwoord.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Wachtwoorden komen niet overeen.")]
    [Display(Name = "Bevestig wachtwoord")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
