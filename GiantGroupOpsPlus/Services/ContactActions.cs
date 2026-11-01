namespace GiantGroupOpsPlus.Services;

/// <summary>Phone, email and maps hand-offs with friendly failure messages.</summary>
public static class ContactActions
{
    public static async Task CallAsync(string? phone, IDialogService dialogs)
    {
        if (string.IsNullOrWhiteSpace(phone)) { await dialogs.AlertAsync("No phone number", "There is no phone number on file for this client."); return; }
        try
        {
            if (PhoneDialer.Default.IsSupported) PhoneDialer.Default.Open(phone.Trim());
            else await dialogs.AlertAsync("Can't place calls", "This device can't make phone calls. Number: " + phone);
        }
        catch (Exception) { await dialogs.AlertAsync("Couldn't open the dialer", "Number: " + phone); }
    }

    public static async Task EmailAsync(string? email, IDialogService dialogs)
    {
        if (string.IsNullOrWhiteSpace(email)) { await dialogs.AlertAsync("No email address", "There is no email address on file for this client."); return; }
        try
        {
            await Email.Default.ComposeAsync(new EmailMessage { To = new List<string> { email.Trim() } });
        }
        catch (Exception) { await dialogs.AlertAsync("No email app found", "Address: " + email); }
    }

    public static async Task DirectionsAsync(string? address, double? lat, double? lng, IDialogService dialogs)
    {
        var query = lat.HasValue && lng.HasValue
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{lat.Value},{lng.Value}")
            : address;
        if (string.IsNullOrWhiteSpace(query)) { await dialogs.AlertAsync("No location", "There is no site address for this item."); return; }
        try
        {
            // Opens the Maps app when installed, otherwise the browser – works on every platform.
            await Launcher.Default.OpenAsync(new Uri("https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query)));
        }
        catch (Exception) { await dialogs.AlertAsync("Couldn't open maps", query); }
    }
}
