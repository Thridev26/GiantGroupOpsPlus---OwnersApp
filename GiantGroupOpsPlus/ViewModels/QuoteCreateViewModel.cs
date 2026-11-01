using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

/// <summary>Shows what a quote for a request would cost (same maths as the website) and lets a manager issue it.</summary>
public partial class QuoteCreateViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int _requestId;

    public QuoteCreateViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<QuoteLine> Lines { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasPreview))] private QuotePreview? _preview;
    [ObservableProperty] private string _servicesText = "";
    [ObservableProperty] private string _travelText = "";
    [ObservableProperty] private string _subtotalText = "";
    [ObservableProperty] private string _vatText = "";
    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private bool _canCreate;
    [ObservableProperty] private bool _hasBlocker;
    [ObservableProperty] private bool _hasExisting;

    public bool HasPreview => Preview != null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var v)) _requestId = Convert.ToInt32(v);
    }

    public Task OnAppearingAsync() => LoadAsync(showBusy: Preview == null);

    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            var p = await _api.GetQuotePreviewAsync(_requestId);
            Preview = p;
            Lines.Clear();
            foreach (var l in p.Lines) Lines.Add(l);
            ServicesText = Money(p.Services); TravelText = Money(p.Travel); SubtotalText = Money(p.Subtotal);
            VatText = Money(p.Vat); TotalText = Money(p.Total);
            CanCreate = p.CanCreate;
            HasBlocker = !string.IsNullOrEmpty(p.Blocker);
            HasExisting = p.QuoteId.HasValue;
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (Preview == null || !CanCreate) return;
        if (!await _dialogs.ConfirmAsync("Issue quote", $"Create and send a quote for {TotalText} to {Preview.ClientName}?", "Create quote")) return;
        QuoteDetail? created = null;
        var ok = await RunAsync(async () => created = await _api.CreateQuoteAsync(_requestId));
        if (ok && created != null)
        {
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("quotedetail", new ShellNavigationQueryParameters { ["id"] = created.QuoteId });
        }
    }

    [RelayCommand]
    private async Task ViewExistingAsync()
    {
        if (Preview?.QuoteId is int id)
        {
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("quotedetail", new ShellNavigationQueryParameters { ["id"] = id });
        }
    }
}
