using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class QuotesViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _searchCts;
    private string? _statusFilter;

    public QuotesViewModel(IApiClient api)
    {
        _api = api;
        Filters.Add(new FilterOption("All", null, OnFilter) { IsSelected = true });
        foreach (var s in new[] { "Sent", "Accepted", "Pending", "Declined", "Rejected" }) Filters.Add(new FilterOption(s, s, OnFilter));
    }

    public ObservableCollection<FilterOption> Filters { get; } = new();
    public ObservableCollection<QuoteSummary> Quotes { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _summary = "";

    public Task OnAppearingAsync() => LoadAsync(showBusy: Quotes.Count == 0);

    private void OnFilter(FilterOption o)
    {
        _statusFilter = o.Value;
        foreach (var f in Filters) f.IsSelected = f == o;
        _ = LoadAsync(true);
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        _ = Task.Delay(450, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync(false));
        });
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            var list = await _api.GetQuotesAsync(_statusFilter, SearchText);
            Quotes.Clear();
            foreach (var q in list) Quotes.Add(q);
            IsEmpty = Quotes.Count == 0;
            var total = Quotes.Sum(q => q.Amount);
            Summary = $"{Quotes.Count} quote{(Quotes.Count == 1 ? "" : "s")} · {Money(total)}";
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private Task OpenAsync(QuoteSummary? q) =>
        q == null ? Task.CompletedTask : Shell.Current.GoToAsync("quotedetail", new ShellNavigationQueryParameters { ["id"] = q.QuoteId });
}
