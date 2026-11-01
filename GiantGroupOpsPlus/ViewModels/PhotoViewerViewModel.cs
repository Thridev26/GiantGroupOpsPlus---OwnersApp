using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class PhotoViewerViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IPhotoCache _cache;
    private readonly IDialogService _dialogs;
    private PhotoItem? _item;

    public PhotoViewerViewModel(IApiClient api, IPhotoCache cache, IDialogService dialogs)
    {
        _api = api;
        _cache = cache;
        _dialogs = dialogs;
    }

    [ObservableProperty] private ImageSource? _image;
    [ObservableProperty] private string _photoType = "";
    [ObservableProperty] private string _caption = "";
    [ObservableProperty] private bool _isLoadingImage = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("photo", out var p) && p is PhotoItem item)
        {
            _item = item;
            PhotoType = item.PhotoType;
            Caption = $"{(item.Server?.JobId != null ? "Job" : "Request")} photo · {item.DateText}";
            _ = LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        if (_item?.Server == null) return;
        IsLoadingImage = true;
        try
        {
            var path = await _cache.GetAsync(_item.PhotoId, thumbnail: false);
            Image = path != null ? ImageSource.FromFile(path) : _item.Image;   // fall back to the thumbnail we already have
            if (path == null && Image == null) ErrorMessage = "This photo isn't available on the server (it may have been taken on the desktop app).";
        }
        catch (ApiException ex) when (ex.IsAuth) { }
        catch (Exception) { ErrorMessage = "Couldn't load the photo."; }
        finally { IsLoadingImage = false; }
    }

    [RelayCommand]
    private async Task ToggleTypeAsync()
    {
        if (_item?.Server == null) return;
        var newType = PhotoType == "BEFORE" ? "AFTER" : "BEFORE";
        await RunAsync(async () =>
        {
            var updated = await _api.UpdatePhotoTypeAsync(_item.PhotoId, newType);
            PhotoType = updated.PhotoType.ToUpperInvariant();
            _item.PhotoType = PhotoType;
            ShowBanner($"Marked as {PhotoType.ToLowerInvariant()} photo");
        });
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_item?.Server == null) return;
        if (!await _dialogs.ConfirmAsync("Delete photo", "Delete this photo permanently? This also removes it for everyone else.", "Delete")) return;
        var ok = await RunAsync(async () =>
        {
            await _api.DeletePhotoAsync(_item.PhotoId);
            _cache.Remove(_item.PhotoId);
        });
        if (ok) await Shell.Current.GoToAsync("..");
    }
}
