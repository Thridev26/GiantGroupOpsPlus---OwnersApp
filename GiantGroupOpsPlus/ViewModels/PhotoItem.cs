using CommunityToolkit.Mvvm.ComponentModel;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

/// <summary>A tile in the photo grid: either a photo already on the server or one still in the upload queue.</summary>
public partial class PhotoItem : ObservableObject
{
    public PhotoItem(SitePhoto server)
    {
        Server = server;
        _photoType = server.PhotoType.ToUpperInvariant();
    }

    public PhotoItem(QueuedPhoto queued)
    {
        Queued = queued;
        _photoType = queued.PhotoType.ToUpperInvariant();
        _image = ImageSource.FromFile(queued.LocalPath);
        RefreshQueueState();
    }

    public SitePhoto? Server { get; }
    public QueuedPhoto? Queued { get; }

    public bool IsQueued => Queued != null;
    public int PhotoId => Server?.PhotoId ?? 0;
    public DateTime Date => Server?.UploadDate ?? Queued?.CreatedUtc.ToLocalTime() ?? DateTime.Now;
    public string DateText => Date.ToString("d MMM, HH:mm");

    [ObservableProperty] private string _photoType;
    [ObservableProperty] private ImageSource? _image;
    [ObservableProperty] private string _queueText = "";
    [ObservableProperty] private bool _isFailed;

    public bool HasImage => Image != null;
    public bool ShowPlaceholder => Image == null;

    partial void OnImageChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(ShowPlaceholder));
    }

    public void RefreshQueueState()
    {
        if (Queued == null) return;
        IsFailed = Queued.State == UploadState.Failed;
        QueueText = Queued.State switch
        {
            UploadState.Uploading => "Uploading…",
            UploadState.Failed => "Failed – tap",
            _ => "Waiting…"
        };
    }

    public async Task LoadThumbnailAsync(IPhotoCache cache, bool full = false)
    {
        if (Server == null) return;
        try
        {
            var path = await cache.GetAsync(Server.PhotoId, thumbnail: !full);
            if (path != null) Image = ImageSource.FromFile(path);
        }
        catch (Exception) { /* leave placeholder */ }
    }
}
