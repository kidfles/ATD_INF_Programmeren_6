using System.Text.Json;

namespace FestivalTickets.Web.Models;

public static class WizardSessionHelper
{
    private const string Key = "BookingWizard";

    public static BookingWizardState Load(ISession session)
    {
        var json = session.GetString(Key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BookingWizardState();
        }

        return JsonSerializer.Deserialize<BookingWizardState>(json) ?? new BookingWizardState();
    }

    public static void Save(ISession session, BookingWizardState state)
    {
        session.SetString(Key, JsonSerializer.Serialize(state));
    }

    public static void Clear(ISession session)
    {
        session.Remove(Key);
    }
}
