using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

/// <summary>Registered as IApiClient: routes each call to the live server or the built-in demo data, depending on Settings.</summary>
public class ApiClientRouter : IApiClient
{
    private readonly ApiClient _live;
    private readonly DemoApiClient _demo;
    private readonly ISettingsService _settings;

    public ApiClientRouter(ApiClient live, DemoApiClient demo, ISettingsService settings)
    {
        _live = live; _demo = demo; _settings = settings;
    }

    private IApiClient C => _settings.DemoMode ? _demo : _live;

    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default) => C.GetDashboardAsync(ct);
    public Task<OverviewDto> GetOverviewAsync(CancellationToken ct = default) => C.GetOverviewAsync(ct);
    public Task<List<JobRequestSummary>> GetRequestsAsync(string? s, string? q, CancellationToken ct = default) => C.GetRequestsAsync(s, q, ct);
    public Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default) => C.GetRequestAsync(id, ct);
    public Task<JobRequestDetail> SetRequestStatusAsync(int id, string s, CancellationToken ct = default) => C.SetRequestStatusAsync(id, s, ct);
    public Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> v, CancellationToken ct = default) => C.SaveDetailsAsync(id, v, ct);
    public Task SetEvaluationDateAsync(int requestId, DateTime date, CancellationToken ct = default) => C.SetEvaluationDateAsync(requestId, date, ct);
    public Task<int> CreateRequestAsync(NewRequestInput input, CancellationToken ct = default) => C.CreateRequestAsync(input, ct);
    public Task<List<JobSummary>> GetJobsAsync(string? s, string? q, CancellationToken ct = default) => C.GetJobsAsync(s, q, ct);
    public Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default) => C.GetJobAsync(id, ct);
    public Task<JobDetail> SetJobStatusAsync(int id, string s, decimal? f, decimal? d, CancellationToken ct = default) => C.SetJobStatusAsync(id, s, f, d, ct);
    public Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget t, CancellationToken ct = default) => C.GetPhotosAsync(t, ct);
    public Task<SitePhoto> UploadPhotoAsync(PhotoTarget t, string type, string path, CancellationToken ct = default) => C.UploadPhotoAsync(t, type, path, ct);
    public Task<SitePhoto> UpdatePhotoTypeAsync(int id, string type, CancellationToken ct = default) => C.UpdatePhotoTypeAsync(id, type, ct);
    public Task DeletePhotoAsync(int id, CancellationToken ct = default) => C.DeletePhotoAsync(id, ct);
    public Task<byte[]?> DownloadPhotoAsync(int id, int? w, CancellationToken ct = default) => C.DownloadPhotoAsync(id, w, ct);

    public Task<LookupsDto> GetLookupsAsync(CancellationToken ct = default) => C.GetLookupsAsync(ct);
    public Task<List<CalendarEvent>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken ct = default) => C.GetCalendarAsync(from, to, ct);

    public Task<List<ClientSummary>> GetClientsAsync(string? search, CancellationToken ct = default) => C.GetClientsAsync(search, ct);
    public Task<ClientDetail> GetClientAsync(int id, CancellationToken ct = default) => C.GetClientAsync(id, ct);
    public Task<ClientDetail> SaveClientAsync(int? id, ClientInput input, CancellationToken ct = default) => C.SaveClientAsync(id, input, ct);

    public Task<List<QuoteSummary>> GetQuotesAsync(string? status, string? search, CancellationToken ct = default) => C.GetQuotesAsync(status, search, ct);
    public Task<QuoteDetail> GetQuoteAsync(int id, CancellationToken ct = default) => C.GetQuoteAsync(id, ct);
    public Task<QuotePreview> GetQuotePreviewAsync(int requestId, CancellationToken ct = default) => C.GetQuotePreviewAsync(requestId, ct);
    public Task<QuoteDetail> CreateQuoteAsync(int requestId, CancellationToken ct = default) => C.CreateQuoteAsync(requestId, ct);
    public Task<QuoteDetail> UpdateQuoteAsync(int id, string? status, decimal? amount, DateTime? expiry, CancellationToken ct = default) => C.UpdateQuoteAsync(id, status, amount, expiry, ct);

    public Task<List<StaffAllocation>> GetStaffAllocationsAsync(string? search, CancellationToken ct = default) => C.GetStaffAllocationsAsync(search, ct);
    public Task<List<AssetAllocation>> GetAssetAllocationsAsync(string? search, CancellationToken ct = default) => C.GetAssetAllocationsAsync(search, ct);
    public Task<JobAllocations> GetJobAllocationsAsync(int jobId, CancellationToken ct = default) => C.GetJobAllocationsAsync(jobId, ct);
    public Task<AssignOptions> GetAssignOptionsAsync(int jobId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default) => C.GetAssignOptionsAsync(jobId, date, slotIds, ct);
    public Task<JobAllocations> AssignStaffAsync(int jobId, int staffId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default) => C.AssignStaffAsync(jobId, staffId, date, slotIds, ct);
    public Task<JobAllocations> AssignAssetAsync(int jobId, string kind, int assetId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default) => C.AssignAssetAsync(jobId, kind, assetId, date, slotIds, ct);
    public Task UnassignStaffAsync(int jobId, int staffId, CancellationToken ct = default) => C.UnassignStaffAsync(jobId, staffId, ct);
    public Task UnassignAssetAsync(int assignmentId, CancellationToken ct = default) => C.UnassignAssetAsync(assignmentId, ct);

    public Task<List<PaymentJob>> GetPaymentsAsync(string? filter, string? search, CancellationToken ct = default) => C.GetPaymentsAsync(filter, search, ct);
    public Task<JobPayments> GetJobPaymentsAsync(int jobId, CancellationToken ct = default) => C.GetJobPaymentsAsync(jobId, ct);
    public Task<JobPayments> RecordPaymentAsync(int jobId, decimal amount, string method, string? reference, CancellationToken ct = default) => C.RecordPaymentAsync(jobId, amount, method, reference, ct);
    public Task<ReportDto> GetReportAsync(CancellationToken ct = default) => C.GetReportAsync(ct);
}
