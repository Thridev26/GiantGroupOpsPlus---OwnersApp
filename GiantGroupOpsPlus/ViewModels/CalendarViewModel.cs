using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class CalendarViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private string? _kindFilter;
    private List<CalendarEvent> _all = new();

    public CalendarViewModel(IApiClient api)
    {
        _api = api;
        Filters.Add(new FilterOption("All", null, OnFilter) { IsSelected = true });
        Filters.Add(new FilterOption("Jobs", "job", OnFilter));
        Filters.Add(new FilterOption("Site evaluations", "evaluation", OnFilter));
    }

    public ObservableCollection<FilterOption> Filters { get; } = new();
    public ObservableCollection<CalendarDay> Days { get; } = new();

    [ObservableProperty] private string _monthText = "";
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private bool _isEmpty;

    public Task OnAppearingAsync() => LoadAsync(showBusy: Days.Count == 0);

    private void OnFilter(FilterOption o)
    {
        _kindFilter = o.Value;
        foreach (var f in Filters) f.IsSelected = f == o;
        Rebuild();
    }

    [RelayCommand] private Task PreviousAsync() { _month = _month.AddMonths(-1); return LoadAsync(true); }
    [RelayCommand] private Task NextAsync() { _month = _month.AddMonths(1); return LoadAsync(true); }
    [RelayCommand] private Task TodayAsync() { _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); return LoadAsync(true); }
    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy)
    {
        MonthText = _month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        await RunAsync(async () =>
        {
            _all = await _api.GetCalendarAsync(_month, _month.AddMonths(1));
            Rebuild();
        }, showBusy, asLoad: true);
    }

    private void Rebuild()
    {
        var list = _all.Where(e => _kindFilter == null || e.Kind == _kindFilter).ToList();
        Days.Clear();
        foreach (var g in list.GroupBy(e => e.Date.Date).OrderBy(g => g.Key)) Days.Add(new CalendarDay(g.Key, g));
        IsEmpty = Days.Count == 0;
        var jobs = list.Count(e => e.IsJob);
        var evals = list.Count - jobs;
        Summary = $"{jobs} job{(jobs == 1 ? "" : "s")} · {evals} site evaluation{(evals == 1 ? "" : "s")}";
    }

    [RelayCommand]
    private Task OpenAsync(CalendarEvent? e)
    {
        if (e == null) return Task.CompletedTask;
        return e.IsJob
            ? Shell.Current.GoToAsync("jobdetail", new ShellNavigationQueryParameters { ["id"] = e.Id })
            : Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = e.Id });
    }
}
