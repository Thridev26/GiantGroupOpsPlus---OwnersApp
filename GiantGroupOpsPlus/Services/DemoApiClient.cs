using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

/// <summary>
/// In-memory fake backend (Settings → Demo mode). Lets you explore every screen and test the photo queue
/// without a server. Enforces the same rules as MobileApi.ashx.
/// </summary>
public partial class DemoApiClient : IApiClient
{
    private readonly object _lock = new();
    private readonly List<JobRequestDetail> _requests = new();
    private readonly List<JobDetail> _jobs = new();
    private readonly List<SitePhoto> _photos = new();
    private readonly Dictionary<int, string> _photoFiles = new();
    private int _nextPhotoId = 100;

    public DemoApiClient() { Seed(); SeedPlus(); }

    private static Task Latency() => Task.Delay(280);

    // ---- dashboard
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            return new DashboardDto
            {
                Pending = _requests.Count(r => r.Status == Statuses.Pending),
                ToEvaluate = _requests.Count(r => r.Status == Statuses.EvaluationDateSet),
                Evaluated = _requests.Count(r => r.Status == Statuses.SiteEvaluated),
                NotStarted = _jobs.Count(j => j.Status == Statuses.NotStarted),
                InProgress = _jobs.Count(j => j.Status == Statuses.InProgress),
                CompletedWeek = _jobs.Count(j => j.Status == Statuses.Completed),
                UpcomingEvaluations = _requests.Where(r => r.SiteEvaluationDate >= DateTime.Today && (r.Status == Statuses.EvaluationDateSet || r.Status == Statuses.Pending))
                                               .OrderBy(r => r.SiteEvaluationDate).Take(5).Select(Summary).ToList(),
                ActiveJobs = _jobs.Where(j => j.Status != Statuses.Completed).Take(8).Select(Summary).ToList()
            };
        }
    }

    // ---- requests
    public async Task<List<JobRequestSummary>> GetRequestsAsync(string? status, string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            return _requests
                .Where(r => string.IsNullOrEmpty(status) || r.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                .Where(r => string.IsNullOrWhiteSpace(search) || r.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || r.SiteAddress.Contains(search, StringComparison.OrdinalIgnoreCase) || r.JobRequestId.ToString() == search.Trim())
                .OrderByDescending(r => r.DateReceived).Select(Summary).ToList();
        }
    }

    public async Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) return Clone(Find(id));
    }

    public async Task<JobRequestDetail> SetRequestStatusAsync(int id, string status, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var r = Find(id);
            if (!r.AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new ApiException($"You are not allowed to set the status '{status}'.", 403);
            if ((status == Statuses.SiteEvaluated || status == Statuses.EvaluationDateSet) && r.SiteEvaluationDate == null)
                throw new ApiException("This request has no site evaluation date yet. The Systems Manager must set it first.", 409);
            r.Status = status;
            return Clone(r);
        }
    }

    public async Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> values, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var r = Find(id);
            foreach (var v in values)
            {
                var field = r.Items.FirstOrDefault(i => i.RequestItemId == v.RequestItemId)?.Details.FirstOrDefault(d => d.JobDetailId == v.JobDetailId)
                            ?? throw new ApiException("A detail field does not belong to this request.", 400);
                if (string.IsNullOrWhiteSpace(v.Value)) continue;
                var err = Validate(v.Value.Trim(), field.DataType);
                if (err != null) throw new ApiException($"'{v.Value}' {err}", 400);
                field.Value = v.Value.Trim();
            }
            return Clone(r);
        }
    }

    private static string? Validate(string v, string type) => type.ToLowerInvariant() switch
    {
        "int" => int.TryParse(v, out _) ? null : "must be a whole number (e.g. 5)",
        "decimal" => decimal.TryParse(v, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _) ? null : "must be a number (e.g. 12.50)",
        "boolean" => new[] { "yes", "no", "true", "false" }.Contains(v.ToLowerInvariant()) ? null : "must be Yes or No",
        _ => null
    };

    // ---- jobs
    public async Task<List<JobSummary>> GetJobsAsync(string? status, string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            return _jobs
                .Where(j => string.IsNullOrEmpty(status) || j.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                .Where(j => string.IsNullOrWhiteSpace(search) || j.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || j.SiteAddress.Contains(search, StringComparison.OrdinalIgnoreCase) || j.JobId.ToString() == search.Trim())
                .Select(Summary).ToList();
        }
    }

    public async Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) return Clone(FindJob(id));
    }

    public async Task<JobDetail> SetJobStatusAsync(int id, string status, decimal? fuelCost, decimal? dumpingCost, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var j = FindJob(id);
            if (!Statuses.AllJob.Contains(status)) throw new ApiException("Invalid job status.", 400);
            j.Status = status;
            j.EndDate = status == Statuses.Completed ? (j.EndDate ?? DateTime.Now) : null;
            if (fuelCost.HasValue) j.FuelCost = fuelCost.Value;
            if (dumpingCost.HasValue) j.DumpingCost = dumpingCost.Value;
            return Clone(j);
        }
    }

    // ---- photos
    public async Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget target, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
            return _photos.Where(p => target.Kind == PhotoTargetKind.Job ? p.JobId == target.Id : p.JobRequestId == target.Id && p.JobId == null)
                          .OrderByDescending(p => p.UploadDate).ToList();
    }

    public async Task<SitePhoto> UploadPhotoAsync(PhotoTarget target, string photoType, string filePath, CancellationToken ct = default)
    {
        await Task.Delay(900, ct);
        lock (_lock)
        {
            var reqId = target.Kind == PhotoTargetKind.Job ? FindJob(target.Id).JobRequestId : Find(target.Id).JobRequestId;
            var p = new SitePhoto
            {
                PhotoId = _nextPhotoId++, PhotoType = photoType, UploadDate = DateTime.Now,
                JobId = target.Kind == PhotoTargetKind.Job ? target.Id : null, JobRequestId = reqId
            };
            // keep a private copy so the demo can show what was "uploaded"
            var keep = Path.Combine(FileSystem.CacheDirectory, $"demo_{p.PhotoId}.jpg");
            File.Copy(filePath, keep, true);
            _photoFiles[p.PhotoId] = keep;
            _photos.Add(p);
            Bump(target, +1);
            return p;
        }
    }

    public async Task<SitePhoto> UpdatePhotoTypeAsync(int photoId, string photoType, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var p = _photos.FirstOrDefault(x => x.PhotoId == photoId) ?? throw new ApiException("Photo not found.", 404);
            p.PhotoType = photoType;
            return p;
        }
    }

    public async Task DeletePhotoAsync(int photoId, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var p = _photos.FirstOrDefault(x => x.PhotoId == photoId) ?? throw new ApiException("Photo not found.", 404);
            _photos.Remove(p);
            Bump(p.JobId.HasValue ? new PhotoTarget(PhotoTargetKind.Job, p.JobId.Value) : new PhotoTarget(PhotoTargetKind.Request, p.JobRequestId), -1);
        }
    }

    public Task<byte[]?> DownloadPhotoAsync(int photoId, int? width, CancellationToken ct = default)
    {
        string? path;
        lock (_lock) _photoFiles.TryGetValue(photoId, out path);
        return Task.FromResult<byte[]?>(path != null && File.Exists(path) ? File.ReadAllBytes(path) : null);
    }

    // ---- helpers
    private JobRequestDetail Find(int id) => _requests.FirstOrDefault(r => r.JobRequestId == id) ?? throw new ApiException("Request not found.", 404);
    private JobDetail FindJob(int id) => _jobs.FirstOrDefault(j => j.JobId == id) ?? throw new ApiException("Job not found.", 404);

    private void Bump(PhotoTarget t, int delta)
    {
        if (t.Kind == PhotoTargetKind.Job) { var j = FindJob(t.Id); j.PhotoCount = Math.Max(0, j.PhotoCount + delta); }
        else { var r = Find(t.Id); r.PhotoCount = Math.Max(0, r.PhotoCount + delta); }
    }

    private static JobRequestSummary Summary(JobRequestDetail r) => new()
    {
        JobRequestId = r.JobRequestId, ClientName = r.ClientName, ClientPhone = r.ClientPhone, ClientEmail = r.ClientEmail, SiteAddress = r.SiteAddress,
        Latitude = r.Latitude, Longitude = r.Longitude, Urgency = r.Urgency, Source = r.Source, Status = r.Status, DateReceived = r.DateReceived,
        SiteEvaluationDate = r.SiteEvaluationDate, RequestedItems = r.RequestedItems, PhotoCount = r.PhotoCount
    };

    private static JobSummary Summary(JobDetail j) => new()
    {
        JobId = j.JobId, JobRequestId = j.JobRequestId, ClientName = j.ClientName, ClientPhone = j.ClientPhone, SiteAddress = j.SiteAddress,
        Latitude = j.Latitude, Longitude = j.Longitude, Status = j.Status, StartDate = j.StartDate, EndDate = j.EndDate, FuelCost = j.FuelCost,
        DumpingCost = j.DumpingCost, LabourCost = j.LabourCost, QuoteId = j.QuoteId, RequestedItems = j.RequestedItems, PhotoCount = j.PhotoCount
    };

    private static JobRequestDetail Clone(JobRequestDetail r)
    {
        var c = new JobRequestDetail();
        foreach (var p in typeof(JobRequestDetail).GetProperties().Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)) p.SetValue(c, p.GetValue(r));
        c.Items = r.Items.Select(i => new RequestItem { RequestItemId = i.RequestItemId, JobTypeName = i.JobTypeName,
            Details = i.Details.Select(d => new DetailField { JobDetailId = d.JobDetailId, Name = d.Name, DataType = d.DataType, Value = d.Value }).ToList() }).ToList();
        c.Jobs = r.Jobs.Select(j => new JobRef { JobId = j.JobId, Status = j.Status }).ToList();
        c.AllowedStatuses = r.AllowedStatuses.ToList();
        return c;
    }

    private static JobDetail Clone(JobDetail j)
    {
        var c = new JobDetail();
        foreach (var p in typeof(JobDetail).GetProperties().Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)) p.SetValue(c, p.GetValue(j));
        c.Staff = j.Staff.Select(s => new StaffAssignment { Name = s.Name, HoursWorked = s.HoursWorked }).ToList();
        c.AllowedStatuses = j.AllowedStatuses.ToList();
        return c;
    }

    private void Seed()
    {
        var today = DateTime.Today;
        var allowed = Statuses.AllRequest.ToList();

        JobRequestDetail R(int id, string client, string phone, string addr, string status, int daysAgo, int? evalInDays, string urgency, string source, double lat, double lng, params RequestItem[] items) => new()
        {
            JobRequestId = id, ClientName = client, ClientPhone = phone, ClientEmail = client.ToLower().Replace(' ', '.') + "@example.com", SiteAddress = addr,
            Status = status, DateReceived = today.AddDays(-daysAgo), SiteEvaluationDate = evalInDays.HasValue ? today.AddDays(evalInDays.Value) : null,
            Urgency = urgency, Source = source, Latitude = lat, Longitude = lng, Items = items.ToList(), AllowedStatuses = allowed,
            RequestedItems = string.Join(", ", items.Select(i => i.JobTypeName).OrderBy(x => x))
        };

        RequestItem Felling(int itemId) => new() { RequestItemId = itemId, JobTypeName = "Tree Felling", Details = new()
        {
            new() { JobDetailId = 1, Name = "Number of trees", DataType = "int" },
            new() { JobDetailId = 2, Name = "Average height (m)", DataType = "decimal" },
            new() { JobDetailId = 3, Name = "Near power lines", DataType = "boolean" },
            new() { JobDetailId = 4, Name = "Access notes", DataType = "varchar" }
        } };
        RequestItem Grass(int itemId) => new() { RequestItemId = itemId, JobTypeName = "Grass Cutting", Details = new()
        {
            new() { JobDetailId = 6, Name = "Area (m²)", DataType = "decimal" },
            new() { JobDetailId = 7, Name = "Grass height (cm)", DataType = "int" }
        } };
        RequestItem Hedge(int itemId) => new() { RequestItemId = itemId, JobTypeName = "Hedge Trimming", Details = new()
        {
            new() { JobDetailId = 18, Name = "Length (m)", DataType = "decimal" },
            new() { JobDetailId = 19, Name = "Height (m)", DataType = "decimal" }
        } };

        _requests.AddRange(new[]
        {
            R(1041, "Thandi Nkosi", "082 555 0141", "14 Jacaranda Rd, Kloof", Statuses.EvaluationDateSet, 3, 1, "High", "Whatsapp", -29.78, 30.83, Felling(1)),
            R(1040, "Pieter van Wyk", "083 555 0140", "7 Ridge Ave, Hillcrest", Statuses.Pending, 2, null, "Medium", "Phone call", -29.78, 30.76, Grass(2), Hedge(3)),
            R(1039, "Aisha Patel", "071 555 0139", "22 Palm Close, Umhlanga", Statuses.EvaluationDateSet, 5, 3, "Low", "Walk in", -29.72, 31.08, Hedge(4)),
            R(1038, "Greenview Body Corporate", "031 555 0138", "Greenview Estate, Westville", Statuses.SiteEvaluated, 9, -2, "Medium", "Other", -29.83, 30.92, Felling(5), Grass(6)),
            R(1037, "Sipho Dlamini", "084 555 0137", "3 Station Rd, Pinetown", Statuses.Cancelled, 14, -7, "Low", "Phone call", -29.81, 30.86, Grass(7)),
        });
        _requests[0].PhotoCount = 0;

        JobDetail J(int id, int reqId, string client, string phone, string addr, string status, int startInDays, decimal fuel, decimal dump, string items, double lat, double lng, params (string, decimal?)[] staff) => new()
        {
            JobId = id, JobRequestId = reqId, ClientName = client, ClientPhone = phone, SiteAddress = addr, Status = status, StartDate = today.AddDays(startInDays),
            FuelCost = fuel, DumpingCost = dump, LabourCost = 1800m, QuoteId = 500 + id, RequestedItems = items, Latitude = lat, Longitude = lng,
            Staff = staff.Select(s => new StaffAssignment { Name = s.Item1, HoursWorked = s.Item2 }).ToList(), AllowedStatuses = Statuses.AllJob.ToList(),
            EndDate = status == Statuses.Completed ? today.AddDays(startInDays) : null
        };
        _jobs.AddRange(new[]
        {
            J(301, 1038, "Greenview Body Corporate", "031 555 0138", "Greenview Estate, Westville", Statuses.InProgress, 0, 640m, 350m, "Grass Cutting, Tree Felling", -29.83, 30.92, ("Mandla Zulu", 6m), ("Ryan Govender", 6m)),
            J(300, 1030, "Naidoo Residence", "082 555 0130", "9 Oak Lane, Durban North", Statuses.NotStarted, 2, 0m, 0m, "Tree Felling", -29.79, 31.02, ("Mandla Zulu", null)),
            J(299, 1028, "Hilltop Primary School", "031 555 0128", "Hilltop Rd, Kloof", Statuses.Completed, -3, 410m, 220m, "Hedge Trimming", -29.79, 30.84, ("Ryan Govender", 8m)),
        });
        _requests.First(r => r.JobRequestId == 1038).Jobs = new() { new JobRef { JobId = 301, Status = Statuses.InProgress } };
    }
}
