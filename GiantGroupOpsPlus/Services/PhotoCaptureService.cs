using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

public interface IPhotoCaptureService
{
    /// <summary>Walks the user through taking/choosing photos for a target and queues them for upload. Returns how many were queued.</summary>
    Task<int> CaptureAsync(PhotoTarget target);
}

public class PhotoCaptureService : IPhotoCaptureService
{
    private const string Camera = "Take photo";
    private const string Gallery = "Choose from gallery";
    private const string Before = "Before";
    private const string After = "After";

    private readonly IPhotoQueueService _queue;
    private readonly IDialogService _dialogs;

    public PhotoCaptureService(IPhotoQueueService queue, IDialogService dialogs)
    {
        _queue = queue;
        _dialogs = dialogs;
    }

    public async Task<int> CaptureAsync(PhotoTarget target)
    {
        var options = MediaPicker.Default.IsCaptureSupported ? new[] { Camera, Gallery } : new[] { Gallery };
        var source = await _dialogs.ChooseAsync($"Add photo to {target.Label}", options);
        if (source == null) return 0;

        var type = await _dialogs.ChooseAsync("What does this photo show?", new[] { Before, After });
        if (type == null) return 0;

        var count = 0;
        while (true)
        {
            string? picked;
            try
            {
                picked = await PickAsync(source);
            }
            catch (PermissionException)
            {
                await _dialogs.AlertAsync("Permission needed", source == Camera
                    ? "Allow camera access for Giant Group Ops in your phone's Settings to take site photos."
                    : "Allow photo access for Giant Group Ops in your phone's Settings to choose photos.");
                break;
            }
            catch (FeatureNotSupportedException)
            {
                await _dialogs.AlertAsync("Not available", "This device can't do that. Try choosing from the gallery instead.");
                break;
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync("Couldn't get the photo", ex.Message);
                break;
            }

            if (picked == null) break;   // user cancelled

            try
            {
                var prepared = await PrepareAsync(picked);
                await _queue.EnqueueAsync(target, type.ToUpperInvariant(), prepared);
                count++;
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync("Couldn't save the photo", ex.Message);
                break;
            }

            var next = await _dialogs.ChooseAsync($"{count} photo{(count == 1 ? "" : "s")} queued for upload",
                new[] { $"Another {type.ToLowerInvariant()} photo", type == Before ? "Another after photo" : "Another before photo" }, "Done");
            if (next == null) break;
            type = next.Contains("before", StringComparison.OrdinalIgnoreCase) ? Before : After;
        }
        return count;
    }

    private static async Task<string?> PickAsync(string source)
    {
        FileResult? result = source == Camera
            ? await MediaPicker.Default.CapturePhotoAsync()
            : await MediaPicker.Default.PickPhotoAsync();
        if (result == null) return null;

        // Copy to our own temp file: the picker's path can be a content:// stream or a cache entry the OS reclaims.
        var temp = Path.Combine(FileSystem.CacheDirectory, $"pick_{Guid.NewGuid():N}{Path.GetExtension(result.FileName)}");
        await using (var src = await result.OpenReadAsync())
        await using (var dst = File.Create(temp))
            await src.CopyToAsync(dst);
        return temp;
    }

    private static async Task<string> PrepareAsync(string rawPath)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "uploads");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, $"{Guid.NewGuid():N}.jpg");
        try
        {
            await ImageProcessor.PrepareAsync(rawPath, dest);
        }
        finally
        {
            try { File.Delete(rawPath); } catch (Exception) { }
        }

        if (new FileInfo(dest).Length > 14 * 1024 * 1024)
            throw new InvalidOperationException("That photo is too large to upload (over 14 MB). Try the camera instead of a screenshot or edited image.");
        return dest;
    }
}
