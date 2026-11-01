using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class PhotosViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IPhotoCache _cache;
    private readonly IPhotoQueueService _queue;
    private readonly IPhotoCaptureService _capture;
    private readonly IDialogService _dialogs;
    private PhotoTarget _target = new(PhotoTargetKind.Request, 0);
    private List<SitePhoto> _server = new();
    private string? _typeFilter;
    private bool _wired;

    public PhotosViewModel(IApiClient api, IPhotoCache cache, IPhotoQueueService queue, IPhotoCaptureService capture, IDialogService dialogs)
    {
        _api = api;
        _cache = cache;
        _queue = queue;
        _capture = capture;
        _dialogs = dialogs;
        Filters.Add(new FilterOption("All", null, OnFilter) { IsSelected = true });
        Filters.Add(new FilterOption("Before", "BEFORE", OnFilter));
        Filters.Add(new FilterOption("After", "AFTER", OnFilter));
    }

    public ObservableCollection<FilterOption> Filters { get; } = new();
    public ObservableCollection<PhotoItem> Photos { get; } = new();

    [ObservableProperty] private string _title = "Photos";
    [ObservableProperty] private string _countsText = "";
    [ObservableProperty] private bool _isEmpty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var kind = query.TryGetValue("kind", out var k) && string.Equals(k?.ToString(), nameof(PhotoTargetKind.Job), StringComparison.OrdinalIgnoreCase)
            ? PhotoTargetKind.Job : PhotoTargetKind.Request;
        var id = query.TryGetValue("id", out var v) ? Convert.ToInt32(v) : 0;
        _target = new PhotoTarget(kind, id);
        Title = $"Photos · {_target.Label}";
    }

    public async Task OnAppearingAsync()
    {
        if (!_wired)
        {
            _wired = true;
            _queue.Changed += OnQueueChanged;
            _queue.Uploaded += OnUploaded;
        }
        await LoadAsync(showBusy: _server.Count == 0 && Photos.Count == 0);
    }

    public void OnDisappearing()
    {
        if (!_wired) return;
        _wired = false;
        _queue.Changed -= OnQueueChanged;
        _queue.Uploaded -= OnUploaded;
    }

    private void OnQueueChanged() => Rebuild();
    private void OnUploaded(QueuedPhoto q, SitePhoto p)
    {
        if (q.Kind == _target.Kind && q.TargetId == _target.Id) _ = LoadAsync(showBusy: false);
    }

    private void OnFilter(FilterOption option)
    {
        _typeFilter = option.Value;
        foreach (var f in Filters) f.IsSelected = f == option;
        Rebuild();
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(showBusy: false);

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showBusy: true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            _server = await _api.GetPhotosAsync(_target);
            Rebuild();
        }, showBusy, asLoad: true);
    }

    /// <summary>Merges server photos with the local upload queue, newest first, honouring the Before/After filter.</summary>
    private void Rebuild()
    {
        var queued = _queue.For(_target).Select(q => new PhotoItem(q)).ToList();
        var uploaded = _server.Select(s => new PhotoItem(s)).ToList();

        var all = queued.Concat(uploaded).ToList();
        var shown = all.Where(p => _typeFilter == null || p.PhotoType == _typeFilter).OrderByDescending(p => p.IsQueued).ThenByDescending(p => p.Date).ToList();

        // Reuse existing tiles where possible so thumbnails don't flicker/reload on every queue tick.
        var existing = Photos.Where(p => !p.IsQueued).ToDictionary(p => p.PhotoId);
        Photos.Clear();
        foreach (var p in shown)
        {
            if (!p.IsQueued && existing.TryGetValue(p.PhotoId, out var keep))
            {
                keep.PhotoType = p.PhotoType;
                Photos.Add(keep);
            }
            else
            {
                Photos.Add(p);
                if (!p.IsQueued) _ = p.LoadThumbnailAsync(_cache);
            }
        }

        var before = all.Count(p => p.PhotoType == "BEFORE");
        var after = all.Count(p => p.PhotoType == "AFTER");
        CountsText = all.Count == 0 ? "" : $"{before} before · {after} after";
        IsEmpty = Photos.Count == 0;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var n = await _capture.CaptureAsync(_target);
        if (n > 0) ShowBanner(n == 1 ? "Photo saved – uploading in the background" : $"{n} photos saved – uploading in the background");
    }

    [RelayCommand]
    private async Task OpenAsync(PhotoItem? item)
    {
        if (item == null) return;

        if (item.Queued != null)
        {
            var q = item.Queued;
            var options = new List<string>();
            if (q.State == UploadState.Failed) options.Add("Retry upload");
            options.Add("Remove from queue");
            var choice = await _dialogs.ChooseAsync(q.State == UploadState.Failed ? $"Upload failed: {q.LastError}" : "Photo waiting to upload", options);
            if (choice == "Retry upload") await _queue.RetryAsync(q.Id);
            else if (choice == "Remove from queue" && await _dialogs.ConfirmAsync("Remove photo", "This photo has not been uploaded yet and will be lost.", "Remove"))
                await _queue.DiscardAsync(q.Id);
            return;
        }

        await Shell.Current.GoToAsync("photoviewer", new ShellNavigationQueryParameters { ["photo"] = item });
    }
}
