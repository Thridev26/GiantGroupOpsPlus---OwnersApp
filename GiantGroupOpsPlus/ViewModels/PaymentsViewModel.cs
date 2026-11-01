using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class PaymentsViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _searchCts;
    private string? _filter;

    public PaymentsViewModel(IApiClient api)
    {
        _api = api;
        Filters.Add(new FilterOption("All", null, OnFilter) { IsSelected = true });
        Filters.Add(new FilterOption("Outstanding", "outstanding", OnFilter));
        Filters.Add(new FilterOption("Paid", "paid", OnFilter));
    }

    public ObservableCollection<FilterOption> Filters { get; } = new();
    public ObservableCollection<PaymentJob> Jobs { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _summary = "";

    public Task OnAppearingAsync() => LoadAsync(showBusy: Jobs.Count == 0);

    private void OnFilter(FilterOption o)
    {
        _filter = o.Value;
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
            var list = await _api.GetPaymentsAsync(_filter, SearchText);
            Jobs.Clear(); foreach (var j in list) Jobs.Add(j);
            IsEmpty = Jobs.Count == 0;
            Summary = $"{Jobs.Count} job{(Jobs.Count == 1 ? "" : "s")} · {Money(Jobs.Sum(j => j.Balance > 0 ? j.Balance : 0))} outstanding";
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private Task OpenAsync(PaymentJob? j) =>
        j == null ? Task.CompletedTask : Shell.Current.GoToAsync("paymentdetail", new ShellNavigationQueryParameters { ["jobId"] = j.JobId });
}
