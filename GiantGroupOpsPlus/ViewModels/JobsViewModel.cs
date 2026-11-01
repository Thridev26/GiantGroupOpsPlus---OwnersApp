using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class JobsViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private readonly AppState _state;
    private CancellationTokenSource? _searchCts;
    private string? _statusFilter;

    public JobsViewModel(IApiClient api, AppState state)
    {
        _api = api;
        _state = state;
        Filters.Add(new FilterOption("All", null, OnFilter) { IsSelected = true });
        foreach (var s in Statuses.AllJob) Filters.Add(new FilterOption(s, s, OnFilter));
    }

    public ObservableCollection<FilterOption> Filters { get; } = new();
    public ObservableCollection<JobSummary> Jobs { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _summary = "";

    public async Task OnAppearingAsync()
    {
        if (_state.JobFilter != null)
        {
            SelectFilter(_state.JobFilter);
            _state.JobFilter = null;
        }
        await LoadAsync(showBusy: Jobs.Count == 0);
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
            var list = await _api.GetJobsAsync(_statusFilter, SearchText);
            Jobs.Clear();
            foreach (var j in list) Jobs.Add(j);
            IsEmpty = Jobs.Count == 0;
            Summary = Jobs.Count == 1 ? "1 job" : $"{Jobs.Count} jobs";
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private Task OpenAsync(JobSummary? j) =>
        j == null ? Task.CompletedTask : Shell.Current.GoToAsync("jobdetail", new ShellNavigationQueryParameters { ["id"] = j.JobId });
}
