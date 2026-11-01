using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

/// <summary>A question that expands to show its answer when tapped.</summary>
public partial class FaqItem : ObservableObject
{
    public FaqItem(string q, string a) { Question = q; Answer = a; }
    public string Question { get; }
    public string Answer { get; }
    [ObservableProperty] private bool _isOpen;
    public string Arrow => IsOpen ? "–" : "+";
    partial void OnIsOpenChanged(bool value) => OnPropertyChanged(nameof(Arrow));

    [RelayCommand] private void Toggle() => IsOpen = !IsOpen;
}

public partial class HelpViewModel : BaseViewModel
{
    public const string Phone = "0848 331 373";
    private readonly IDialogService _dialogs;
    private readonly ISettingsService _settings;

    public HelpViewModel(IDialogService dialogs, ISettingsService settings)
    {
        _dialogs = dialogs;
        _settings = settings;
        Faq.Add(new FaqItem("Where does The Giant Group work?", "We are based in Queensburgh and service Durban, KwaZulu-Natal and the surrounding areas."));
        Faq.Add(new FaqItem("What services do we offer?", "Tree felling and dismantling, stump removal, site clearing, hedge trimming, grass cutting, tree planting and general garden and property maintenance."));
        Faq.Add(new FaqItem("How does a job request move along?", "Pending → Evaluation Date Set → Site Evaluated → Quote Sent → Quote Accepted. A request can be Cancelled at any point. Capture the site measurements during the evaluation so a quote can be calculated."));
        Faq.Add(new FaqItem("How is a quotation worked out?", "Each requested service is priced from its rate × the measured quantity. A travel fee is added (call-out plus the round-trip distance from our base), then 15% VAT. If a site has no GPS pin a flat travel fee is used."));
        Faq.Add(new FaqItem("How do I book staff or equipment?", "Open a job and tap Assign staff or Assign equipment. Pick the date and time slots – people and equipment already booked for those slots are flagged so you can't double-book them."));
        Faq.Add(new FaqItem("How do I add before / after photos?", "Open a request or job and tap Take or add photos. Photos are saved on your phone first and upload in the background, so they are safe even without signal."));
        Faq.Add(new FaqItem("A client paid by cash or EFT – where do I record it?", "More → Invoicing & payments → choose the job → Record a payment. Online (PayFast) payments are recorded automatically."));
    }

    public ObservableCollection<FaqItem> Faq { get; } = new();

    [RelayCommand] private Task CallAsync() => ContactActions.CallAsync(Phone, _dialogs);

    [RelayCommand]
    private Task WhatsAppAsync() => OpenAsync("https://wa.me/27848331373?text=" + Uri.EscapeDataString("Hello The Giant Group"));

    [RelayCommand]
    private Task InstagramAsync() => OpenAsync("https://www.instagram.com/thegiantgroup_/");

    [RelayCommand]
    private Task WebsiteAsync()
    {
        var url = _settings.ServerUrl;
        var i = url.IndexOf("/MobileApi.ashx", StringComparison.OrdinalIgnoreCase);
        return OpenAsync(i > 0 ? url.Substring(0, i) : url);
    }

    private async Task OpenAsync(string url)
    {
        try { await Launcher.Default.OpenAsync(new Uri(url)); }
        catch (Exception) { await _dialogs.AlertAsync("Couldn't open the link", url); }
    }
}
