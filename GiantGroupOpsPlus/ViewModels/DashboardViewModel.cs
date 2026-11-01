using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private readonly IPhotoQueueService _queue;
    private readonly AppState _state;

    public DashboardViewModel(IApiClient api, IPhotoQueueService queue, AppState state)
    {
        _api = api;
        _queue = queue;
        _state = state;
        _queue.Changed += () => { OnPropertyChanged(nameof(QueueText)); OnPropertyChanged(nameof(HasQueue)); };
    }

    [ObservableProperty] private string _greeting = "";
    [ObservableProperty] private string _today = DateTime.Today.ToString("dddd, d MMMM");
    [ObservableProperty] private int _pending;
    [ObservableProperty] private int _toEvaluate;
    [ObservableProperty] private int _evaluated;
    [ObservableProperty] private int _notStarted;
    [ObservableProperty] private int _inProgress;
    [ObservableProperty] private int _completedWeek;
    [ObservableProperty] private bool _hasUpcoming;
    [ObservableProperty] private bool _hasActiveJobs;
    [ObservableProperty] private bool _hasOverview;
    [ObservableProperty] private int _quotesSent;
    [ObservableProperty] private int _readyToQuote;
    [ObservableProperty] private int _jobsWithoutCrew;
    [ObservableProperty] private string _outstandingText = "";

    public ObservableCollection<JobRequestSummary> Upcoming { get; } = new();
    public ObservableCollection<JobSummary> ActiveJobs { get; } = new();

    public bool HasQueue => _queue.OutstandingCount > 0;
    public string QueueText
    {
        get
        {
            var n = _queue.OutstandingCount;
            return n == 1 ? "1 photo waiting to upload" : $"{n} photos waiting to upload";
        }
    }

    public async Task OnAppearingAsync()
    {
        var hour = DateTime.Now.Hour;
        var part = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        Greeting = part;
        Today = DateTime.Today.ToString("dddd, d MMMM");
        OnPropertyChanged(nameof(QueueText));
        OnPropertyChanged(nameof(HasQueue));
        await LoadAsync(showBusy: Upcoming.Count == 0 && ActiveJobs.Count == 0);
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(showBusy: false);

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showBusy: true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            var d = await _api.GetDashboardAsync();
            Pending = d.Pending; ToEvaluate = d.ToEvaluate; Evaluated = d.Evaluated;
            NotStarted = d.NotStarted; InProgress = d.InProgress; CompletedWeek = d.CompletedWeek;

            Upcoming.Clear(); foreach (var r in d.UpcomingEvaluations) Upcoming.Add(r);
            ActiveJobs.Clear(); foreach (var j in d.ActiveJobs) ActiveJobs.Add(j);
            HasUpcoming = Upcoming.Count > 0;
            HasActiveJobs = ActiveJobs.Count > 0;
        }, showBusy, asLoad: true);

        // Business tiles are an extra: if the server can't provide them the rest of the dashboard still works.
        try
        {
            var o = await _api.GetOverviewAsync();
            QuotesSent = o.QuotesSent; ReadyToQuote = o.ReadyToQuote; JobsWithoutCrew = o.JobsWithoutCrew;
            OutstandingText = Money(o.Outstanding);
            HasOverview = true;
        }
        catch (Exception) { HasOverview = false; }
    }

    [RelayCommand]
    private Task OpenRequestAsync(JobRequestSummary? r) =>
        r == null ? Task.CompletedTask : Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = r.JobRequestId });

    [RelayCommand]
    private Task OpenJobAsync(JobSummary? j) =>
        j == null ? Task.CompletedTask : Shell.Current.GoToAsync("jobdetail", new ShellNavigationQueryParameters { ["id"] = j.JobId });

    [RelayCommand]
    private Task ShowRequestsAsync(string? status)
    {
        _state.RequestFilter = string.IsNullOrEmpty(status) ? null : status;
        return Shell.Current.GoToAsync("//requests");
    }

    [RelayCommand]
    private Task ShowJobsAsync(string? status)
    {
        _state.JobFilter = string.IsNullOrEmpty(status) ? null : status;
        return Shell.Current.GoToAsync("//jobs");
    }

    [RelayCommand]
    private Task OpenQueueAsync() => Shell.Current.GoToAsync("queue");

    [RelayCommand]
    private Task OpenSectionAsync(string? route) =>
        string.IsNullOrEmpty(route) ? Task.CompletedTask : Shell.Current.GoToAsync(route);

    [RelayCommand]
    private Task ShowReadyToQuoteAsync()
    {
        _state.RequestFilter = Statuses.SiteEvaluated;
        return Shell.Current.GoToAsync("//requests");
    }

    [RelayCommand]
    private Task NewRequestAsync() => Shell.Current.GoToAsync("newrequest");
}
