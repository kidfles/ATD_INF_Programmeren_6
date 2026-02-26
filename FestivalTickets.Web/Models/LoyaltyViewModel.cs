namespace FestivalTickets.Web.Models;

public class LoyaltyViewModel
{
    public int    CustomerId    { get; set; }
    public string FullName      { get; set; } = string.Empty;
    public string Email         { get; set; } = string.Empty;
    public bool   HasLoyaltyCard { get; set; }
}
