using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class QuoteDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int _id;

    public QuoteDetailViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<QuoteLine> Lines { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasDetail))] private QuoteDetail? _detail;
    [ObservableProperty] private string _servicesText = "";
    [ObservableProperty] private string _travelText = "";
    [ObservableProperty] private string _travelNote = "";
    [ObservableProperty] private string _subtotalText = "";
    [ObservableProperty] private string _vatText = "";
    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private bool _amountEdited;
    [ObservableProperty] private string _editedText = "";

    public bool HasDetail => Detail != null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var v)) _id = Convert.ToInt32(v);
    }

    public Task OnAppearingAsync() => LoadAsync(showBusy: Detail == null);

    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy) =>
        await RunAsync(async () => Apply(await _api.GetQuoteAsync(_id)), showBusy, asLoad: true);

    private void Apply(QuoteDetail d)
    {
        Detail = d;
        Lines.Clear();
        foreach (var l in d.Breakdown.Lines) Lines.Add(l);
        ServicesText = Money(d.Breakdown.Services);
        TravelText = Money(d.Breakdown.Travel);
        TravelNote = d.Breakdown.TravelNote ?? "";
        SubtotalText = Money(d.Breakdown.Subtotal);
        VatText = Money(d.Breakdown.Vat);
        TotalText = Money(d.Amount);
        AmountEdited = d.AmountEdited;
        EditedText = d.AmountEdited ? $"The saved amount was changed from the calculated {Money(d.Breakdown.Total)}." : "";
    }

    [RelayCommand]
    private async Task ChangeStatusAsync()
    {
        if (Detail == null) return;
        var options = Detail.AllowedStatuses.Where(s => !s.Equals(Detail.Status, StringComparison.OrdinalIgnoreCase)).ToList();
        var choice = await _dialogs.ChooseAsync($"Quote status (now: {Detail.Status})", options);
        if (choice == null) return;
        var note = choice is "Accepted" or "Sent" ? "\n\nThe job request status will be updated to match." : choice is "Rejected" or "Declined" ? "\n\nThe job request will be marked Cancelled." : "";
        if (!await _dialogs.ConfirmAsync("Update quote", $"Set quote #{Detail.QuoteId} to “{choice}”?{note}", "Update")) return;
        await RunAsync(async () =>
        {
            Apply(await _api.UpdateQuoteAsync(_id, choice, null, null));
            ShowBanner($"Quote marked {choice}");
        });
    }

    [RelayCommand]
    private async Task EditAmountAsync()
    {
        if (Detail == null) return;
        var text = await _dialogs.PromptAsync("Quote amount (incl. VAT)", "Enter the new total in rand.", Detail.Amount.ToString("0.00", CultureInfo.InvariantCulture), "0.00", Keyboard.Numeric);
        if (text == null) return;
        if (!decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0 || amount > 100_000_000m)
        {
            await _dialogs.AlertAsync("Check the amount", "Enter a valid amount, e.g. 4500.00");
            return;
        }
        await RunAsync(async () =>
        {
            Apply(await _api.UpdateQuoteAsync(_id, null, amount, null));
            ShowBanner("Amount updated");
        });
    }

    [RelayCommand]
    private async Task ExtendExpiryAsync()
    {
        if (Detail == null) return;
        var choice = await _dialogs.ChooseAsync("Extend quote expiry", new[] { "+7 days", "+14 days", "+30 days" });
        if (choice == null) return;
        var days = int.Parse(choice.Substring(1, choice.IndexOf(' ') - 1), CultureInfo.InvariantCulture);
        var from = Detail.ExpiryDate.HasValue && Detail.ExpiryDate.Value.Date > DateTime.Today ? Detail.ExpiryDate.Value.Date : DateTime.Today;
        await RunAsync(async () =>
        {
            Apply(await _api.UpdateQuoteAsync(_id, null, null, from.AddDays(days)));
            ShowBanner("Expiry date updated");
        });
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (Detail == null) return;
        var d = Detail;
        var lines = string.Join("\n", d.Breakdown.Lines.Select(l => $"  {l.Name}: {l.Detail} = {l.TotalText}"));
        var text = $"The Giant Group – Quotation #{d.QuoteId}\nClient: {d.ClientName}\nSite: {d.SiteAddress}\n\n{lines}\n  Travel: {TravelText}\n  VAT (15%): {VatText}\n\nTotal: {TotalText}\n{d.ExpiryText}";
        await Share.Default.RequestAsync(new ShareTextRequest { Text = text, Title = $"Quotation #{d.QuoteId}" });
    }

    [RelayCommand] private Task CallAsync() => ContactActions.CallAsync(Detail?.ClientPhone, _dialogs);
    [RelayCommand] private Task EmailAsync() => ContactActions.EmailAsync(Detail?.ClientEmail, _dialogs);

    [RelayCommand]
    private Task OpenRequestAsync() =>
        Detail == null ? Task.CompletedTask : Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = Detail.RequestId });
}
