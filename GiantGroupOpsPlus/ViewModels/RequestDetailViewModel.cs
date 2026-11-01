using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

/// <summary>One editable measurement on a requested item (e.g. "Average height (m)").</summary>
public partial class FieldEditor : ObservableObject
{
    public FieldEditor(int requestItemId, DetailField f)
    {
        RequestItemId = requestItemId;
        JobDetailId = f.JobDetailId;
        Name = f.Name;
        DataType = (f.DataType ?? "varchar").ToLowerInvariant();
        Original = f.Value ?? "";
        _value = Original;
    }

    public int RequestItemId { get; }
    public int JobDetailId { get; }
    public string Name { get; }
    public string DataType { get; }
    public string Original { get; }

    [ObservableProperty] private string _value;

    public bool IsBoolean => DataType == "boolean";
    public bool IsText => !IsBoolean;
    public bool IsDirty => !string.Equals((Value ?? "").Trim(), Original.Trim(), StringComparison.Ordinal);
    public string Hint => DataType switch
    {
        "int" => "Whole number (e.g. 5)",
        "decimal" => "Number (e.g. 12.50)",
        "date" => "Date (dd/MM/yyyy)",
        "boolean" => "Yes or No",
        _ => "Text (max 50 characters)"
    };
    public Keyboard Keyboard => DataType switch
    {
        "int" => Keyboard.Numeric,
        "decimal" => Keyboard.Numeric,
        _ => Keyboard.Text
    };
    public string[] YesNo { get; } = { "Yes", "No" };
}

public partial class ItemEditor : ObservableObject
{
    public ItemEditor(string title, IEnumerable<FieldEditor> fields)
    {
        Title = title;
        Fields = new ObservableCollection<FieldEditor>(fields);
    }
    public string Title { get; }
    public ObservableCollection<FieldEditor> Fields { get; }
    public bool HasFields => Fields.Count > 0;
}

public partial class RequestDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private readonly IPhotoCaptureService _capture;
    private readonly IPhotoQueueService _queue;
    private int _id;
    private bool _wired;

    public RequestDetailViewModel(IApiClient api, IDialogService dialogs, IPhotoCaptureService capture, IPhotoQueueService queue)
    {
        _api = api;
        _dialogs = dialogs;
        _capture = capture;
        _queue = queue;
    }

    public ObservableCollection<ItemEditor> Items { get; } = new();
    public ObservableCollection<JobRef> Jobs { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetail))]
    private JobRequestDetail? _detail;

    [ObservableProperty] private bool _hasJobs;
    [ObservableProperty] private bool _hasItems;
    [ObservableProperty] private string _quickActionText = "";
    [ObservableProperty] private bool _hasQuickAction;
    [ObservableProperty] private string _queuedText = "";
    [ObservableProperty] private bool _hasQueued;
    [ObservableProperty] private DateTime _evalDate = DateTime.Today.AddDays(1);

    public bool HasDetail => Detail != null;

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
        if (q.Kind == PhotoTargetKind.Request && q.TargetId == _id) _ = LoadAsync(showBusy: false);
    }

    private void UpdateQueued()
    {
        var n = _queue.For(new PhotoTarget(PhotoTargetKind.Request, _id)).Count;
        HasQueued = n > 0;
        QueuedText = n == 1 ? "1 photo uploading…" : $"{n} photos uploading…";
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(showBusy: false);

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showBusy: true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () => Apply(await _api.GetRequestAsync(_id)), showBusy, asLoad: true);
    }

    private void Apply(JobRequestDetail d)
    {
        Detail = d;
        if (d.SiteEvaluationDate.HasValue) EvalDate = d.SiteEvaluationDate.Value.Date;
        Items.Clear();
        foreach (var item in d.Items)
            Items.Add(new ItemEditor(item.JobTypeName, item.Details.Select(f => new FieldEditor(item.RequestItemId, f))));
        HasItems = Items.Count > 0;

        Jobs.Clear();
        foreach (var j in d.Jobs) Jobs.Add(j);
        HasJobs = Jobs.Count > 0;

        // One-tap shortcut for the most common field action
        var canEvaluate = d.AllowedStatuses.Contains(Statuses.SiteEvaluated, StringComparer.OrdinalIgnoreCase);
        var done = string.Equals(d.Status, Statuses.SiteEvaluated, StringComparison.OrdinalIgnoreCase);
        HasQuickAction = canEvaluate && !done && d.SiteEvaluationDate.HasValue
                         && !string.Equals(d.Status, Statuses.Cancelled, StringComparison.OrdinalIgnoreCase);
        QuickActionText = "Mark site as evaluated";
    }

    // ---- status
    [RelayCommand]
    private async Task ChangeStatusAsync()
    {
        if (Detail == null) return;
        var options = Detail.AllowedStatuses.Where(s => !s.Equals(Detail.Status, StringComparison.OrdinalIgnoreCase)).ToList();
        var choice = await _dialogs.ChooseAsync($"Change status (now: {Detail.Status})", options);
        if (choice != null) await SetStatusAsync(choice);
    }

    [RelayCommand]
    private Task MarkEvaluatedAsync() => SetStatusAsync(Statuses.SiteEvaluated);

    private async Task SetStatusAsync(string status)
    {
        if (Detail == null) return;

        if ((status == Statuses.SiteEvaluated || status == Statuses.EvaluationDateSet) && !Detail.SiteEvaluationDate.HasValue)
        {
            await _dialogs.AlertAsync("No evaluation date", "This request has no site evaluation date yet. The Systems Manager must set it before the site can be evaluated.");
            return;
        }

        var unsaved = Items.SelectMany(i => i.Fields).Any(f => f.IsDirty);
        var extra = status == Statuses.SiteEvaluated && unsaved ? "\n\nYou have measurements that are not saved yet – save them first." : "";
        var warn = status == Statuses.Cancelled ? "\n\nCancelling a request can only be reversed by a manager above Operations." : "";
        if (!await _dialogs.ConfirmAsync("Update status", $"Set request #{Detail.JobRequestId} to “{status}”?{warn}{extra}", "Update"))
            return;

        await RunAsync(async () =>
        {
            Apply(await _api.SetRequestStatusAsync(_id, status));
            ShowBanner($"Status updated to {status}");
        });
    }

    // ---- measurements
    [RelayCommand]
    private async Task SaveDetailsAsync()
    {
        var dirty = Items.SelectMany(i => i.Fields).Where(f => f.IsDirty && !string.IsNullOrWhiteSpace(f.Value)).ToList();
        if (dirty.Count == 0)
        {
            ShowBanner("No changes to save");
            return;
        }
        foreach (var f in dirty)
        {
            var value = f.Value.Trim();
            if (value.Length > 50) { await _dialogs.AlertAsync("Too long", $"“{f.Name}” must be 50 characters or fewer."); return; }
            var err = Validate(value, f.DataType);
            if (err != null) { await _dialogs.AlertAsync("Check the value", $"“{value}” {err} for {f.Name}."); return; }
        }

        await RunAsync(async () =>
        {
            var values = dirty.Select(f => new DetailValue { RequestItemId = f.RequestItemId, JobDetailId = f.JobDetailId, Value = f.Value.Trim() });
            Apply(await _api.SaveDetailsAsync(_id, values));
            ShowBanner($"{dirty.Count} measurement{(dirty.Count == 1 ? "" : "s")} saved");
        });
    }

    private static string? Validate(string v, string type)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return type switch
        {
            "int" => int.TryParse(v, System.Globalization.NumberStyles.Integer, inv, out _) ? null : "must be a whole number (e.g. 5)",
            "decimal" => decimal.TryParse(v.Replace(',', '.'), System.Globalization.NumberStyles.Number, inv, out _) ? null : "must be a number (e.g. 12.50)",
            "date" => DateTime.TryParseExact(v, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy" }, inv, System.Globalization.DateTimeStyles.None, out _) ? null : "must be a date (dd/MM/yyyy)",
            "boolean" => new[] { "yes", "no", "true", "false" }.Contains(v.ToLowerInvariant()) ? null : "must be Yes or No",
            _ => null
        };
    }

    // ---- evaluation date + quote (full app)
    [RelayCommand]
    private async Task SetEvaluationDateAsync()
    {
        if (Detail == null) return;
        if (!await _dialogs.ConfirmAsync("Site evaluation date", $"Set the site evaluation for request #{Detail.JobRequestId} to {EvalDate:dddd d MMMM yyyy}?", "Set date")) return;
        var ok = await RunAsync(async () =>
        {
            await _api.SetEvaluationDateAsync(_id, EvalDate);
            Apply(await _api.GetRequestAsync(_id));
            ShowBanner("Evaluation date saved");
        });
    }

    [RelayCommand]
    private Task OpenQuoteAsync() =>
        Shell.Current.GoToAsync("quotecreate", new ShellNavigationQueryParameters { ["requestId"] = _id });

    // ---- photos
    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var n = await _capture.CaptureAsync(new PhotoTarget(PhotoTargetKind.Request, _id));
        if (n > 0) ShowBanner(n == 1 ? "Photo saved – uploading in the background" : $"{n} photos saved – uploading in the background");
    }

    [RelayCommand]
    private Task ViewPhotosAsync() =>
        Shell.Current.GoToAsync("photos", new ShellNavigationQueryParameters { ["kind"] = PhotoTargetKind.Request.ToString(), ["id"] = _id });

    // ---- contact / navigation
    [RelayCommand]
    private Task OpenJobAsync(JobRef? j) =>
        j == null ? Task.CompletedTask : Shell.Current.GoToAsync("jobdetail", new ShellNavigationQueryParameters { ["id"] = j.JobId });

    [RelayCommand]
    private Task CallAsync() => ContactActions.CallAsync(Detail?.ClientPhone, _dialogs);

    [RelayCommand]
    private Task EmailAsync() => ContactActions.EmailAsync(Detail?.ClientEmail, _dialogs);

    [RelayCommand]
    private Task DirectionsAsync() => ContactActions.DirectionsAsync(Detail?.SiteAddress, Detail?.Latitude, Detail?.Longitude, _dialogs);
}
