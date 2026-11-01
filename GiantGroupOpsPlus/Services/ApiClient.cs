using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

/// <summary>Talks to MobileApi.ashx on the Giant Group website. There is no sign-in, so no token is sent.</summary>
public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly ISettingsService _settings;

    public ApiClient(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
    }

    // ---- dashboard
    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default) =>
        SendAsync<DashboardDto>(HttpMethod.Get, "dashboard", ct: ct);

    public Task<OverviewDto> GetOverviewAsync(CancellationToken ct = default) =>
        SendAsync<OverviewDto>(HttpMethod.Get, "overview", ct: ct);

    // ---- requests
    public Task<List<JobRequestSummary>> GetRequestsAsync(string? status, string? search, CancellationToken ct = default) =>
        SendAsync<List<JobRequestSummary>>(HttpMethod.Get, "requests" + Query(("status", status), ("search", search)), ct: ct);

    public Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default) =>
        SendAsync<JobRequestDetail>(HttpMethod.Get, $"requests/{id}", ct: ct);

    public Task<JobRequestDetail> SetRequestStatusAsync(int id, string status, CancellationToken ct = default) =>
        SendAsync<JobRequestDetail>(HttpMethod.Post, $"requests/{id}/status", new { status }, retry: false, ct: ct);

    public Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> values, CancellationToken ct = default) =>
        SendAsync<JobRequestDetail>(HttpMethod.Post, $"requests/{id}/details", new { values }, retry: false, ct: ct);

    public async Task SetEvaluationDateAsync(int requestId, DateTime date, CancellationToken ct = default) =>
        await SendAsync<JsonElement>(HttpMethod.Post, $"requests/{requestId}/evaluation-date", new { date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }, retry: false, ct: ct);

    public async Task<int> CreateRequestAsync(NewRequestInput input, CancellationToken ct = default)
    {
        var r = await SendAsync<JsonElement>(HttpMethod.Post, "requests", input, retry: false, ct: ct);
        return r.TryGetProperty("jobRequestId", out var v) ? v.GetInt32() : 0;
    }

    // ---- jobs
    public Task<List<JobSummary>> GetJobsAsync(string? status, string? search, CancellationToken ct = default) =>
        SendAsync<List<JobSummary>>(HttpMethod.Get, "jobs" + Query(("status", status), ("search", search)), ct: ct);

    public Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default) =>
        SendAsync<JobDetail>(HttpMethod.Get, $"jobs/{id}", ct: ct);

    public Task<JobDetail> SetJobStatusAsync(int id, string status, decimal? fuelCost, decimal? dumpingCost, CancellationToken ct = default) =>
        SendAsync<JobDetail>(HttpMethod.Post, $"jobs/{id}/status", new { status, fuelCost, dumpingCost }, retry: false, ct: ct);

    // ---- photos
    public Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget target, CancellationToken ct = default) =>
        SendAsync<List<SitePhoto>>(HttpMethod.Get,
            "photos?" + (target.Kind == PhotoTargetKind.Job ? "jobId=" : "requestId=") + target.Id, ct: ct);

    public async Task<SitePhoto> UploadPhotoAsync(PhotoTarget target, string photoType, string filePath, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var file = new StreamContent(fs);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(photoType), "photoType");
        content.Add(new StringContent(target.Id.ToString()), target.Kind == PhotoTargetKind.Job ? "jobID" : "jobRequestID");

        using var req = Build(HttpMethod.Post, "photos");
        req.Content = content;
        return await ExecuteAsync<SitePhoto>(req, ct, TimeSpan.FromMinutes(3));
    }

    public Task<SitePhoto> UpdatePhotoTypeAsync(int photoId, string photoType, CancellationToken ct = default) =>
        SendAsync<SitePhoto>(HttpMethod.Put, $"photos/{photoId}", new { photoType }, retry: false, ct: ct);

    public async Task DeletePhotoAsync(int photoId, CancellationToken ct = default) =>
        await SendAsync<JsonElement>(HttpMethod.Delete, $"photos/{photoId}", retry: false, ct: ct);

    public async Task<byte[]?> DownloadPhotoAsync(int photoId, int? width, CancellationToken ct = default)
    {
        try
        {
            using var req = Build(HttpMethod.Get, $"photos/{photoId}/file" + (width.HasValue ? $"?w={width}" : ""));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DefaultTimeout);
            using var resp = await _http.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException) { return null; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return null; }
    }

    // ---- lookups + calendar
    public Task<LookupsDto> GetLookupsAsync(CancellationToken ct = default) =>
        SendAsync<LookupsDto>(HttpMethod.Get, "lookups", ct: ct);

    public Task<List<CalendarEvent>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        SendAsync<List<CalendarEvent>>(HttpMethod.Get, "calendar" + Query(("from", Iso(from)), ("to", Iso(to))), ct: ct);

    // ---- clients
    public Task<List<ClientSummary>> GetClientsAsync(string? search, CancellationToken ct = default) =>
        SendAsync<List<ClientSummary>>(HttpMethod.Get, "clients" + Query(("search", search)), ct: ct);

    public Task<ClientDetail> GetClientAsync(int id, CancellationToken ct = default) =>
        SendAsync<ClientDetail>(HttpMethod.Get, $"clients/{id}", ct: ct);

    public Task<ClientDetail> SaveClientAsync(int? id, ClientInput input, CancellationToken ct = default) =>
        SendAsync<ClientDetail>(HttpMethod.Post, id.HasValue ? $"clients/{id.Value}" : "clients", input, retry: false, ct: ct);

    // ---- quotes
    public Task<List<QuoteSummary>> GetQuotesAsync(string? status, string? search, CancellationToken ct = default) =>
        SendAsync<List<QuoteSummary>>(HttpMethod.Get, "quotes" + Query(("status", status), ("search", search)), ct: ct);

    public Task<QuoteDetail> GetQuoteAsync(int id, CancellationToken ct = default) =>
        SendAsync<QuoteDetail>(HttpMethod.Get, $"quotes/{id}", ct: ct);

    public Task<QuotePreview> GetQuotePreviewAsync(int requestId, CancellationToken ct = default) =>
        SendAsync<QuotePreview>(HttpMethod.Get, $"quotes/preview?requestId={requestId}", ct: ct);

    public Task<QuoteDetail> CreateQuoteAsync(int requestId, CancellationToken ct = default) =>
        SendAsync<QuoteDetail>(HttpMethod.Post, "quotes", new { requestId }, retry: false, ct: ct);

    public Task<QuoteDetail> UpdateQuoteAsync(int id, string? status, decimal? amount, DateTime? expiry, CancellationToken ct = default) =>
        SendAsync<QuoteDetail>(HttpMethod.Post, $"quotes/{id}", new { status, amount, expiryDate = expiry.HasValue ? Iso(expiry.Value) : null }, retry: false, ct: ct);

    // ---- allocation
    public Task<List<StaffAllocation>> GetStaffAllocationsAsync(string? search, CancellationToken ct = default) =>
        SendAsync<List<StaffAllocation>>(HttpMethod.Get, "allocations/staff" + Query(("search", search)), ct: ct);

    public Task<List<AssetAllocation>> GetAssetAllocationsAsync(string? search, CancellationToken ct = default) =>
        SendAsync<List<AssetAllocation>>(HttpMethod.Get, "allocations/assets" + Query(("search", search)), ct: ct);

    public Task<JobAllocations> GetJobAllocationsAsync(int jobId, CancellationToken ct = default) =>
        SendAsync<JobAllocations>(HttpMethod.Get, $"jobs/{jobId}/allocations", ct: ct);

    public Task<AssignOptions> GetAssignOptionsAsync(int jobId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default) =>
        SendAsync<AssignOptions>(HttpMethod.Get, $"jobs/{jobId}/options" + Query(("date", Iso(date)), ("slots", string.Join(",", slotIds))), ct: ct);

    public Task<JobAllocations> AssignStaffAsync(int jobId, int staffId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default) =>
        SendAsync<JobAllocations>(HttpMethod.Post, $"jobs/{jobId}/assign-staff", new { staffId, date = Iso(date), slotIds = slotIds.ToArray() }, retry: false, ct: ct);

    public Task<JobAllocations> AssignAssetAsync(int jobId, string kind, int assetId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default) =>
        SendAsync<JobAllocations>(HttpMethod.Post, $"jobs/{jobId}/assign-asset", new { kind, assetId, date = Iso(date), slotIds = slotIds.ToArray() }, retry: false, ct: ct);

    public async Task UnassignStaffAsync(int jobId, int staffId, CancellationToken ct = default) =>
        await SendAsync<JsonElement>(HttpMethod.Post, "allocations/staff/unassign", new { jobId, staffId }, retry: false, ct: ct);

    public async Task UnassignAssetAsync(int assignmentId, CancellationToken ct = default) =>
        await SendAsync<JsonElement>(HttpMethod.Post, "allocations/assets/unassign", new { assignmentId }, retry: false, ct: ct);

    // ---- payments + reports
    public Task<List<PaymentJob>> GetPaymentsAsync(string? filter, string? search, CancellationToken ct = default) =>
        SendAsync<List<PaymentJob>>(HttpMethod.Get, "payments" + Query(("filter", filter), ("search", search)), ct: ct);

    public Task<JobPayments> GetJobPaymentsAsync(int jobId, CancellationToken ct = default) =>
        SendAsync<JobPayments>(HttpMethod.Get, $"payments/job/{jobId}", ct: ct);

    public Task<JobPayments> RecordPaymentAsync(int jobId, decimal amount, string method, string? reference, CancellationToken ct = default) =>
        SendAsync<JobPayments>(HttpMethod.Post, $"payments/job/{jobId}", new { amount, method, reference }, retry: false, ct: ct);

    public Task<ReportDto> GetReportAsync(CancellationToken ct = default) =>
        SendAsync<ReportDto>(HttpMethod.Get, "reports/summary", ct: ct);

    // ---- plumbing
    private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Query(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs.Where(p => !string.IsNullOrWhiteSpace(p.Value))
                         .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!.Trim())}").ToList();
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private HttpRequestMessage Build(HttpMethod method, string path)
    {
        var baseUrl = _settings.ServerUrl.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            throw new ApiException("The server address is not valid. Check it in Settings.");
        var req = new HttpRequestMessage(method, $"{baseUrl}/{path}");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return req;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, bool retry = true, CancellationToken ct = default)
    {
        // Only idempotent reads are retried automatically; writes surface the error so nothing is applied twice.
        var attempts = retry && method == HttpMethod.Get ? 3 : 1;
        for (var i = 1; ; i++)
        {
            try
            {
                using var req = Build(method, path);
                if (body != null) req.Content = JsonContent.Create(body, options: Json);
                return await ExecuteAsync<T>(req, ct);
            }
            catch (ApiException ex) when (i < attempts && ex.IsTransient && !ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400 * i * i), ct);
            }
        }
    }

    private async Task<T> ExecuteAsync<T>(HttpRequestMessage req, CancellationToken ct, TimeSpan? timeout = null)
    {
        HttpResponseMessage resp;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout ?? DefaultTimeout);
        try
        {
            resp = await _http.SendAsync(req, cts.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("The server took too long to respond. Check your signal and try again.", 408, true, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("Can't reach the server. Check your internet connection.", 0, true, ex);
        }

        using (resp)
        {
            if (resp.IsSuccessStatusCode)
            {
                try
                {
                    var result = await resp.Content.ReadFromJsonAsync<T>(Json, ct);
                    return result ?? throw new ApiException("The server returned an empty response.", (int)resp.StatusCode);
                }
                catch (JsonException ex)
                {
                    throw new ApiException("The server sent a response the app could not read. Is the server URL pointing at MobileApi.ashx?", (int)resp.StatusCode, false, ex);
                }
            }

            string message = resp.StatusCode == System.Net.HttpStatusCode.NotFound
                ? "The server doesn't know this feature yet. Make sure the website has been updated with MobileApiV2."
                : "Something went wrong (" + (int)resp.StatusCode + ").";
            try
            {
                var err = await resp.Content.ReadFromJsonAsync<ErrorBody>(Json, ct);
                if (!string.IsNullOrWhiteSpace(err?.Error)) message = err!.Error!;
            }
            catch (Exception) { /* non-JSON error page (e.g. IIS) – keep generic message */ }

            throw new ApiException(message, (int)resp.StatusCode);
        }
    }

    private sealed class ErrorBody { public string? Error { get; set; } }
}
