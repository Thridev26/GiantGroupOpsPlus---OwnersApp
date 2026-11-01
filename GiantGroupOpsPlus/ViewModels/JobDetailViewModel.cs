using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class JobDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private readonly IPhotoCaptureService _capture;
    private readonly IPhotoQueueService _queue;
    private int _id;
    private bool _wired;

    public JobDetailViewModel(IApiClient api, IDialogService dialogs, IPhotoCaptureService capture, IPhotoQueueService queue)
    {
        _api = api;
        _dialogs = dialogs;
        _capture = capture;
        _queue = queue;
        foreach (var s in Statuses.AllJob) StatusOptions.Add(new FilterOption(s, s, OnStatusTapped));
    }

    public ObservableCollection<FilterOption> StatusOptions { get; } = new();
    public ObservableCollection<StaffAssignment> Staff { get; } = new();
    public ObservableCollection<JobStaffAlloc> CrewAlloc { get; } = new();
    public ObservableCollection<JobAssetAlloc> AssetAlloc { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetail))]
    private JobDetail? _detail;

    [ObservableProperty] private string _fuelText = "";
    [ObservableProperty] private string _dumpingText = "";
    [ObservableProperty] private bool _hasStaff;
    [ObservableProperty] private string _queuedText = "";
    [ObservableProperty] private bool _hasQueued;
    [ObservableProperty] private string _totalCostText = "";

    [ObservableProperty] private bool _hasAllocApi;
    [ObservableProperty] private bool _hasCrewAlloc;
    [ObservableProperty] private bool _hasAssetAlloc;

    public bool HasDetail => Detail != null;
    public bool ShowLegacyCrew => HasStaff && !HasAllocApi;
    public bool HasQuote => Detail?.QuoteId != null;
    partial void OnHasStaffChanged(bool value) => OnPropertyChanged(nameof(ShowLegacyCrew));
    partial void OnHasAllocApiChanged(bool value) => OnPropertyChanged(nameof(ShowLegacyCrew));
    partial void OnDetailChanged(JobDetail? value) => OnPropertyChanged(nameof(HasQuote));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var v)) _id = Convert.ToInt32(v);
    }

    public async Task OnAppearingAsync()
    {
        if (!_wired)
        {
            _wired = true;
            _queue.Changed += UpdateQueued;
            _queue.Uploaded += OnUploaded;
        }
        UpdateQueued();
        await LoadAsync(showBusy: Detail == null);
    }

    public void OnDisappearing()
    {
        if (!_wired) return;
        _wired = false;
        _queue.Changed -= UpdateQueued;
        _queue.Uploaded -= OnUploaded;
    }

    private void OnUploaded(QueuedPhoto q, SitePhoto p)
    {
        if (q.Kind == PhotoTargetKind.Job && q.TargetId == _id) _ = LoadAsync(showBusy: false);
    }

    private void UpdateQueued()
    {
        var n = _queue.For(new PhotoTarget(PhotoTargetKind.Job, _id)).Count;
        HasQueued = n > 0;
        QueuedText = n == 1 ? "1 photo uploading…" : $"{n} photos uploading…";
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(showBusy: false);

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showBusy: true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () => Apply(await _api.GetJobAsync(_id)), showBusy, asLoad: true);
        await LoadAllocationsAsync();
    }

    /// <summary>Crew + equipment come from the full-feature API; if the server doesn't have it the old crew list stays.</summary>
    private async Task LoadAllocationsAsync()
    {
        try
        {
            var a = await _api.GetJobAllocationsAsync(_id);
            CrewAlloc.Clear(); foreach (var s in a.Staff) CrewAlloc.Add(s);
            AssetAlloc.Clear(); foreach (var x in a.Assets) AssetAlloc.Add(x);
            HasCrewAlloc = CrewAlloc.Count > 0;
            HasAssetAlloc = AssetAlloc.Count > 0;
            HasAllocApi = true;
        }
        catch (Exception) { /* optional extra – ignore */ }
    }

    [RelayCommand]
    private Task AssignStaffAsync() =>
        Shell.Current.GoToAsync("assign", new ShellNavigationQueryParameters { ["jobId"] = _id, ["mode"] = "staff" });

    [RelayCommand]
    private Task AssignAssetAsync() =>
        Shell.Current.GoToAsync("assign", new ShellNavigationQueryParameters { ["jobId"] = _id, ["mode"] = "asset" });

    [RelayCommand]
    private async Task RemoveStaffAsync(JobStaffAlloc? s)
    {
        if (s == null) return;
        if (!await _dialogs.ConfirmAsync("Remove from job", $"Remove {s.Name} from job #{_id}?", "Remove")) return;
        if (await RunAsync(async () => await _api.UnassignStaffAsync(_id, s.StaffId)))
        {
            ShowBanner($"{s.Name} removed");
            await LoadAllocationsAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveAssetAsync(JobAssetAlloc? a)
    {
        if (a == null) return;
        if (!await _dialogs.ConfirmAsync("Remove equipment", $"Remove {a.Name} from job #{_id}?", "Remove")) return;
        if (await RunAsync(async () => await _api.UnassignAssetAsync(a.AssignmentId)))
        {
            ShowBanner($"{a.Name} removed");
            await LoadAllocationsAsync();
        }
    }

    [RelayCommand]
    private Task OpenPaymentsAsync() =>
        Shell.Current.GoToAsync("paymentdetail", new ShellNavigationQueryParameters { ["jobId"] = _id });

    [RelayCommand]
    private Task OpenQuoteAsync() =>
        Detail?.QuoteId is int q ? Shell.Current.GoToAsync("quotedetail", new ShellNavigationQueryParameters { ["id"] = q }) : Task.CompletedTask;

    private void Apply(JobDetail d)
    {
        Detail = d;
        FuelText = d.FuelCost.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        DumpingText = d.DumpingCost.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        TotalCostText = Money(d.FuelCost + d.DumpingCost + d.LabourCost);
        Staff.Clear();
        foreach (var s in d.Staff) Staff.Add(s);
        HasStaff = Staff.Count > 0;
        foreach (var o in StatusOptions) o.IsSelected = string.Equals(o.Value, d.Status, StringComparison.OrdinalIgnoreCase);
    }

    // ---- status
    private void OnStatusTapped(FilterOption option)
    {
        if (option.Value == null || Detail == null) return;
        if (string.Equals(option.Value, Detail.Status, StringComparison.OrdinalIgnoreCase)) return;
        _ = SetStatusAsync(option.Value);
    }

    private async Task SetStatusAsync(string status)
    {
        if (Detail == null) return;

        var warning = "";
        if (status == Statuses.Completed && Detail.PhotoCount == 0 && !_queue.For(new PhotoTarget(PhotoTargetKind.Job, _id)).Any())
            warning = "\n\nNo site photos have been added to this job yet.";
        if (status == Statuses.Completed && !ValidMoney(FuelText, out _)) { await _dialogs.AlertAsync("Check costs", "Fuel cost must be a valid amount."); return; }

        var ok = await _dialogs.ConfirmAsync("Update job status",
            $"Set job #{Detail.JobId} to “{status}”?{warning}", status == Statuses.Completed ? "Complete job" : "Update");
        if (!ok) return;

        await RunAsync(async () =>
        {
            Apply(await _api.SetJobStatusAsync(_id, status, null, null));
            ShowBanner($"Job marked {status}");
        });
    }

    // ---- costs
    [RelayCommand]
    private async Task SaveCostsAsync()
    {
        if (Detail == null) return;
        if (!ValidMoney(FuelText, out var fuel)) { await _dialogs.AlertAsync("Check costs", "Fuel cost must be a positive number (e.g. 650.50)."); return; }
        if (!ValidMoney(DumpingText, out var dump)) { await _dialogs.AlertAsync("Check costs", "Dumping cost must be a positive number (e.g. 350)."); return; }

        await RunAsync(async () =>
        {
            Apply(await _api.SetJobStatusAsync(_id, Detail.Status, fuel, dump));
            ShowBanner("Costs saved");
        });
    }

    private static bool ValidMoney(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return true;      // blank = 0
        var t = text.Trim().Replace(',', '.');
        return decimal.TryParse(t, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value) && value >= 0 && value <= 10_000_000m;
    }

    // ---- photos
    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var n = await _capture.CaptureAsync(new PhotoTarget(PhotoTargetKind.Job, _id));
        if (n > 0) ShowBanner(n == 1 ? "Photo saved – uploading in the background" : $"{n} photos saved – uploading in the background");
    }

    [RelayCommand]
    private Task ViewPhotosAsync() =>
        Shell.Current.GoToAsync("photos", new ShellNavigationQueryParameters { ["kind"] = PhotoTargetKind.Job.ToString(), ["id"] = _id });

    [RelayCommand]
    private Task OpenRequestAsync() =>
        Detail == null ? Task.CompletedTask : Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = Detail.JobRequestId });

    [RelayCommand]
    private Task CallAsync() => ContactActions.CallAsync(Detail?.ClientPhone, _dialogs);

    [RelayCommand]
    private Task DirectionsAsync() => ContactActions.DirectionsAsync(Detail?.SiteAddress, Detail?.Latitude, Detail?.Longitude, _dialogs);
}
