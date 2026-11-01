using System.Text.Json;
using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

public interface IPhotoQueueService
{
    IReadOnlyList<QueuedPhoto> Items { get; }
    int OutstandingCount { get; }
    event Action? Changed;
    event Action<QueuedPhoto, SitePhoto>? Uploaded;

    Task InitializeAsync();
    IReadOnlyList<QueuedPhoto> For(PhotoTarget target);
    Task EnqueueAsync(PhotoTarget target, string photoType, string preparedFilePath);
    Task ProcessAsync();
    Task RetryAsync(Guid id);
    Task RetryAllAsync();
    Task DiscardAsync(Guid id);
}

/// <summary>
/// Persistent, resumable upload queue. Site photos are written to app storage first and uploaded in the
/// background, so a flat battery, a dead zone or an app restart never loses a photo.
/// </summary>
public class PhotoQueueService : IPhotoQueueService
{
    private static readonly string QueueFile = Path.Combine(FileSystem.AppDataDirectory, "upload-queue.json");

    private readonly IApiClient _api;
    private readonly ISettingsService _settings;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly List<QueuedPhoto> _items = new();
    private int _running;
    private int _retryScheduled;
    private bool _initialized;

    public PhotoQueueService(IApiClient api, ISettingsService settings)
    {
        _api = api;
        _settings = settings;
    }

    public IReadOnlyList<QueuedPhoto> Items { get { lock (_items) return _items.ToList(); } }
    public int OutstandingCount { get { lock (_items) return _items.Count; } }
    public event Action? Changed;
    public event Action<QueuedPhoto, SitePhoto>? Uploaded;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            if (File.Exists(QueueFile))
            {
                var json = await File.ReadAllTextAsync(QueueFile);
                var loaded = JsonSerializer.Deserialize<List<QueuedPhoto>>(json) ?? new();
                lock (_items)
                {
                    foreach (var q in loaded)
                    {
                        if (!File.Exists(q.LocalPath)) continue;          // file vanished – nothing to upload
                        if (q.State == UploadState.Uploading) q.State = UploadState.Pending; // interrupted last run
                        _items.Add(q);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Queue load failed: " + ex);
        }

        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet) _ = ProcessAsync();
        };
        RaiseChanged();
        _ = ProcessAsync();
    }

    public IReadOnlyList<QueuedPhoto> For(PhotoTarget target)
    {
        lock (_items) return _items.Where(i => i.Kind == target.Kind && i.TargetId == target.Id).ToList();
    }

    public async Task EnqueueAsync(PhotoTarget target, string photoType, string preparedFilePath)
    {
        var item = new QueuedPhoto { Kind = target.Kind, TargetId = target.Id, PhotoType = photoType, LocalPath = preparedFilePath };
        lock (_items) _items.Add(item);
        await SaveAsync();
        RaiseChanged();
        _ = ProcessAsync();
    }

    public async Task ProcessAsync()
    {
        if (Interlocked.Exchange(ref _running, 1) == 1) return;   // one worker at a time
        var needsRetryLater = false;
        try
        {
            while (true)
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) { needsRetryLater = HasWaiting(); break; }
                if (_settings.WifiOnlyUploads && !OnUnmeteredNetwork()) break;

                QueuedPhoto? next;
                lock (_items) next = _items.FirstOrDefault(i => i.State == UploadState.Pending);
                if (next == null) break;

                next.State = UploadState.Uploading;
                next.LastError = null;
                RaiseChanged();

                try
                {
                    if (!File.Exists(next.LocalPath)) throw new ApiException("The photo file is no longer on this device.", 404);
                    next.Attempts++;
                    var saved = await _api.UploadPhotoAsync(next.Target, next.PhotoType, next.LocalPath);

                    lock (_items) _items.Remove(next);
                    TryDelete(next.LocalPath);
                    await SaveAsync();
                    RaiseChanged();
                    MainThread.BeginInvokeOnMainThread(() => Uploaded?.Invoke(next, saved));
                }
                catch (ApiException ex) when (ex.IsAuth)
                {
                    next.State = UploadState.Pending;           // keep it; resume after next sign-in
                    await SaveAsync(); RaiseChanged();
                    break;
                }
                catch (ApiException ex) when (ex.IsTransient)
                {
                    next.State = UploadState.Pending;
                    next.LastError = ex.Message;
                    await SaveAsync(); RaiseChanged();
                    needsRetryLater = true;
                    break;                                       // network trouble – stop and back off
                }
                catch (Exception ex)
                {
                    next.State = UploadState.Failed;             // permanent (rejected / deleted target / corrupt)
                    next.LastError = ex.Message;
                    await SaveAsync(); RaiseChanged();
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }

        if (needsRetryLater) ScheduleRetry();
    }

    public async Task RetryAsync(Guid id)
    {
        lock (_items)
        {
            var q = _items.FirstOrDefault(i => i.Id == id);
            if (q != null) { q.State = UploadState.Pending; q.LastError = null; }
        }
        await SaveAsync(); RaiseChanged();
        _ = ProcessAsync();
    }

    public async Task RetryAllAsync()
    {
        lock (_items) foreach (var q in _items.Where(i => i.State == UploadState.Failed)) { q.State = UploadState.Pending; q.LastError = null; }
        await SaveAsync(); RaiseChanged();
        _ = ProcessAsync();
    }

    public async Task DiscardAsync(Guid id)
    {
        QueuedPhoto? q;
        lock (_items)
        {
            q = _items.FirstOrDefault(i => i.Id == id);
            if (q != null) _items.Remove(q);
        }
        if (q != null) TryDelete(q.LocalPath);
        await SaveAsync(); RaiseChanged();
    }

    // ---- helpers
    private bool HasWaiting() { lock (_items) return _items.Any(i => i.State == UploadState.Pending); }

    private static bool OnUnmeteredNetwork()
    {
        var p = Connectivity.Current.ConnectionProfiles;
        return p.Contains(ConnectionProfile.WiFi) || p.Contains(ConnectionProfile.Ethernet);
    }

    private void ScheduleRetry()
    {
        if (Interlocked.Exchange(ref _retryScheduled, 1) == 1) return;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(45));
            Interlocked.Exchange(ref _retryScheduled, 0);
            await ProcessAsync();
        });
    }

    private async Task SaveAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            string json;
            lock (_items) json = JsonSerializer.Serialize(_items);
            var tmp = QueueFile + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, QueueFile, true);               // atomic replace – never leaves a half-written queue
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Queue save failed: " + ex); }
        finally { _fileLock.Release(); }
    }

    private void RaiseChanged() => MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke());

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
    }
}
