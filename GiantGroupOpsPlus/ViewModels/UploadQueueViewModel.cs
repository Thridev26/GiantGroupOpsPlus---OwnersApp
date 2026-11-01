using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class QueueItemViewModel : ObservableObject
{
    private readonly IPhotoQueueService _queue;
    public QueuedPhoto Photo { get; }

    public QueueItemViewModel(QueuedPhoto photo, IPhotoQueueService queue)
    {
        Photo = photo;
        _queue = queue;
        Image = ImageSource.FromFile(photo.LocalPath);
    }

    public ImageSource Image { get; }
    public string Title => $"{Photo.Target.Label} · {Photo.PhotoType.ToLowerInvariant()}";
    public string StateText => Photo.State switch
    {
        UploadState.Uploading => "Uploading…",
        UploadState.Failed => "Failed: " + (Photo.LastError ?? "unknown error"),
        _ => string.IsNullOrEmpty(Photo.LastError) ? "Waiting for a connection…" : "Will retry: " + Photo.LastError
    };
    public string StateColorKey => Photo.State == UploadState.Failed ? "failed" : Photo.State == UploadState.Uploading ? "in progress" : "pending";
    public bool CanRetry => Photo.State != UploadState.Uploading;

    [RelayCommand] private Task RetryAsync() => _queue.RetryAsync(Photo.Id);
    [RelayCommand] private Task DiscardAsync() => _queue.DiscardAsync(Photo.Id);
}

public partial class UploadQueueViewModel : BaseViewModel
{
    private readonly IPhotoQueueService _queue;
    private readonly IDialogService _dialogs;
    private bool _wired;

    public UploadQueueViewModel(IPhotoQueueService queue, IDialogService dialogs)
    {
        _queue = queue;
        _dialogs = dialogs;
    }

    public ObservableCollection<QueueItemViewModel> Items { get; } = new();

    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private bool _hasFailed;

    public void OnAppearing()
    {
        if (!_wired) { _wired = true; _queue.Changed += Rebuild; }
        Rebuild();
        _ = _queue.ProcessAsync();
    }

    public void OnDisappearing()
    {
        if (_wired) { _wired = false; _queue.Changed -= Rebuild; }
    }

    private void Rebuild()
    {
        Items.Clear();
        foreach (var q in _queue.Items.OrderBy(i => i.CreatedUtc)) Items.Add(new QueueItemViewModel(q, _queue));
        IsEmpty = Items.Count == 0;
        HasFailed = _queue.Items.Any(i => i.State == UploadState.Failed);
    }

    [RelayCommand]
    private Task RetryAllAsync() => _queue.RetryAllAsync();
}
