using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class AllocationsViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _searchCts;

    public AllocationsViewModel(IApiClient api)
    {
        _api = api;
        Modes.Add(new FilterOption("Staff", "staff", OnMode) { IsSelected = true });
        Modes.Add(new FilterOption("Equipment", "assets", OnMode));
    }

    public ObservableCollection<FilterOption> Modes { get; } = new();
    public ObservableCollection<StaffAllocation> StaffItems { get; } = new();
    public ObservableCollection<AssetAllocation> AssetItems { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isStaffMode = true;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _summary = "";

    public bool IsAssetMode => !IsStaffMode;

    public Task OnAppearingAsync() => LoadAsync(showBusy: StaffItems.Count == 0 && AssetItems.Count == 0);

    private void OnMode(FilterOption o)
    {
        foreach (var m in Modes) m.IsSelected = m == o;
        IsStaffMode = o.Value == "staff";
        OnPropertyChanged(nameof(IsAssetMode));
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
            if (IsStaffMode)
            {
                var l = await _api.GetStaffAllocationsAsync(SearchText);
                StaffItems.Clear(); foreach (var x in l) StaffItems.Add(x);
                IsEmpty = StaffItems.Count == 0;
                Summary = StaffItems.Count == 1 ? "1 staff booking" : $"{StaffItems.Count} staff bookings";
            }
            else
            {
                var l = await _api.GetAssetAllocationsAsync(SearchText);
                AssetItems.Clear(); foreach (var x in l) AssetItems.Add(x);
                IsEmpty = AssetItems.Count == 0;
                Summary = AssetItems.Count == 1 ? "1 equipment booking" : $"{AssetItems.Count} equipment bookings";
            }
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private Task OpenJobAsync(int jobId) =>
        Shell.Current.GoToAsync("jobdetail", new ShellNavigationQueryParameters { ["id"] = jobId });
}
