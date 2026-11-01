using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settings;
    private readonly IApiClient _api;
    private readonly IPhotoQueueService _queue;
    private readonly IDialogService _dialogs;
    private bool _loading;

    public SettingsViewModel(ISettingsService settings, IApiClient api, IPhotoQueueService queue, IDialogService dialogs)
    {
        _settings = settings;
        _api = api;
        _queue = queue;
        _dialogs = dialogs;
        _queue.Changed += () => OnPropertyChanged(nameof(QueueText));
    }

    [ObservableProperty] private string _userName = "Giant Group Ops";
    [ObservableProperty] private string _roleText = "Full access · Operations & Management";
    [ObservableProperty] private string _serverText = "";
    [ObservableProperty] private bool _wifiOnly;
    [ObservableProperty] private bool _demoMode;

    public string QueueText => _queue.OutstandingCount == 0 ? "Nothing waiting" : $"{_queue.OutstandingCount} waiting";
    public string VersionText => $"Version {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    public void OnAppearing()
    {
        _loading = true;
        ServerText = _settings.DemoMode ? "Demo mode (sample data)" : _settings.ServerUrl;
        WifiOnly = _settings.WifiOnlyUploads;
        DemoMode = _settings.DemoMode;
        _loading = false;
        OnPropertyChanged(nameof(QueueText));
    }

    partial void OnWifiOnlyChanged(bool value)
    {
        if (_loading) return;
        _settings.WifiOnlyUploads = value;
        if (!value) _ = _queue.ProcessAsync();
    }

    partial void OnDemoModeChanged(bool value)
    {
        if (_loading) return;
        _ = SwitchDemoAsync(value);
    }

    private async Task SwitchDemoAsync(bool value)
    {
        var ok = await _dialogs.ConfirmAsync(value ? "Switch to demo mode?" : "Leave demo mode?",
            value ? "The app will show sample data instead of live data." : "The app will show live data from the server.", "Continue");
        if (!ok)
        {
            _loading = true; DemoMode = !value; _loading = false;
            return;
        }
        _settings.DemoMode = value;
        ServerText = value ? "Demo mode (sample data)" : _settings.ServerUrl;
        await Shell.Current.GoToAsync("//dashboard");
    }

    [RelayCommand]
    private Task OpenQueueAsync() => Shell.Current.GoToAsync("queue");

    // ---- full-app sections (pushed pages)
    [RelayCommand] private Task OpenSectionAsync(string? route) => string.IsNullOrEmpty(route) ? Task.CompletedTask : Shell.Current.GoToAsync(route);
    [RelayCommand] private Task NewRequestAsync() => Shell.Current.GoToAsync("newrequest");

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await RunAsync(async () =>
        {
            await _api.GetDashboardAsync();
            ShowBanner("Connected to the Giant Group server");
        });
    }
}
