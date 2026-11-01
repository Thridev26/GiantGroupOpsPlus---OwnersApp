namespace GiantGroupOpsPlus.Services;

public interface IPhotoCache
{
    /// <summary>Local file path of a (thumbnail or full-size) server photo, downloading it on first use. Null if unavailable.</summary>
    Task<string?> GetAsync(int photoId, bool thumbnail, CancellationToken ct = default);
    void Remove(int photoId);
}

/// <summary>
/// Photos need the bearer token, so UriImageSource can't be used. This downloads them once into the cache
/// folder (the OS may evict it) and hands back a file path that Image can display.
/// </summary>
public class PhotoCache : IPhotoCache
{
    private const int ThumbWidth = 360;
    private static readonly string Dir = Path.Combine(FileSystem.CacheDirectory, "photos");
    private readonly IApiClient _api;
    private readonly SemaphoreSlim _gate = new(4, 4);

    public PhotoCache(IApiClient api)
    {
        _api = api;
        Directory.CreateDirectory(Dir);
    }

    public async Task<string?> GetAsync(int photoId, bool thumbnail, CancellationToken ct = default)
    {
        var path = Path.Combine(Dir, $"{photoId}_{(thumbnail ? "t" : "f")}.jpg");
        if (File.Exists(path) && new FileInfo(path).Length > 0) return path;

        await _gate.WaitAsync(ct);
        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > 0) return path;
            var bytes = await _api.DownloadPhotoAsync(photoId, thumbnail ? ThumbWidth : null, ct);
            if (bytes == null || bytes.Length == 0) return null;
            var tmp = path + ".tmp";
            await File.WriteAllBytesAsync(tmp, bytes, ct);
            File.Move(tmp, path, true);
            return path;
        }
        catch (OperationCanceledException) { throw; }
        catch (ApiException) { return null; }
        catch (IOException) { return null; }
        finally { _gate.Release(); }
    }

    public void Remove(int photoId)
    {
        foreach (var suffix in new[] { "t", "f" })
        {
            try { File.Delete(Path.Combine(Dir, $"{photoId}_{suffix}.jpg")); } catch (Exception) { }
        }
    }
}
