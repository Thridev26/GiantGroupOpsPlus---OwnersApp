using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public class BarRow
{
    public string Label { get; init; } = "";
    public string ValueText { get; init; } = "";
    public double Fraction { get; init; }
    public Color BarColor { get; init; } = Colors.Gray;
}

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    public ReportsViewModel(IApiClient api) => _api = api;

    public ObservableCollection<BarRow> Requests { get; } = new();
    public ObservableCollection<BarRow> Jobs { get; } = new();
    public ObservableCollection<BarRow> Quotes { get; } = new();
    public ObservableCollection<BarRow> Months { get; } = new();

    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _hasMonths;
    [ObservableProperty] private string _invoiced = "";
    [ObservableProperty] private string _collected = "";
    [ObservableProperty] private string _outstanding = "";
    [ObservableProperty] private string _collectionRate = "";
    [ObservableProperty] private string _acceptRate = "";
    [ObservableProperty] private string _last30 = "";
    [ObservableProperty] private string _clients = "";

    public Task OnAppearingAsync() => LoadAsync(showBusy: !HasData);

    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private static void Fill(ObservableCollection<BarRow> target, IEnumerable<(string Label, double Value, string Text)> rows)
    {
        target.Clear();
        var list = rows.ToList();
        var max = list.Count == 0 ? 1 : Math.Max(1, list.Max(r => r.Value));
        foreach (var r in list)
            target.Add(new BarRow { Label = r.Label, ValueText = r.Text, Fraction = r.Value / max, BarColor = StatusColors.For(r.Label) });
    }

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            var r = await _api.GetReportAsync();
            Invoiced = Money(r.Invoiced); Collected = Money(r.Collected); Outstanding = Money(r.Outstanding);
            CollectionRate = $"{r.CollectionRate:0.#}%"; AcceptRate = $"{r.QuoteAcceptRate:0.#}%"; Clients = r.Clients.ToString();
            Last30 = r.CollectedLast30Days.HasValue ? Money(r.CollectedLast30Days.Value) : "—";
            Fill(Requests, r.RequestsByStatus.Select(x => (x.Label, (double)x.Count, x.Count.ToString())));
            Fill(Jobs, r.JobsByStatus.Select(x => (x.Label, (double)x.Count, x.Count.ToString())));
            Fill(Quotes, r.QuotesByStatus.Select(x => (x.Label, (double)x.Count, $"{x.Count} · {Money(x.Value)}")));
            Fill(Months, r.PaymentsByMonth.Select(x => (x.Label, (double)x.Value, Money(x.Value))));
            HasMonths = Months.Count > 0;
            HasData = true;
        }, showBusy, asLoad: true);
    }
}
