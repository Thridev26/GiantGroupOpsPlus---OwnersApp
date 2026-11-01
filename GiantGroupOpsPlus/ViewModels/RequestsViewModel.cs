using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class RequestsViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private readonly AppState _state;
    private CancellationTokenSource? _searchCts;
    private string? _statusFilter;

    public RequestsViewModel(IApiClient api, AppState state)
    {
        _api = api;
        _state = state;
        Filters.Add(new FilterOption("All", null, OnFilter) { IsSelected = true });
        foreach (var s in Statuses.AllRequest) Filters.Add(new FilterOption(s, s, OnFilter));
    }

    public ObservableCollection<FilterOption> Filters { get; } = new();
    public ObservableCollection<JobRequestSummary> Requests { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _summary = "";

    public async Task OnAppearingAsync()
    {
        // A dashboard tile may have asked for a specific filter.
        if (_state.RequestFilter != null)
        {
            SelectFilter(_state.RequestFilter);
            _state.RequestFilter = null;
        }
        await LoadAsync(showBusy: Requests.Count == 0);
    }

    private void SelectFilter(string? value)
    {
        _statusFilter = value;
        foreach (var f in Filters) f.IsSelected = string.Equals(f.Value, value, StringComparison.OrdinalIgnoreCase);
    }

    private void OnFilter(FilterOption option)
    {
        SelectFilter(option.Value);
        _ = LoadAsync(showBusy: true);
    }

    // Debounced search-as-you-type
    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        _ = Task.Delay(450, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync(showBusy: false));
        });
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(showBusy: false);

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showBusy: true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            var list = await _api.GetRequestsAsync(_statusFilter, SearchText);
            Requests.Clear();
            foreach (var r in list) Requests.Add(r);
            IsEmpty = Requests.Count == 0;
            Summary = Requests.Count == 1 ? "1 request" : $"{Requests.Count} requests";
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private Task OpenAsync(JobRequestSummary? r) =>
        r == null ? Task.CompletedTask : Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = r.JobRequestId });
}
