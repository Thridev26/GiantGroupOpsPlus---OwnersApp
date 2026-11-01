using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

public interface IApiClient
{
    // ---- dashboard
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<OverviewDto> GetOverviewAsync(CancellationToken ct = default);

    // ---- requests
    Task<List<JobRequestSummary>> GetRequestsAsync(string? status, string? search, CancellationToken ct = default);
    Task<JobRequestDetail> GetRequestAsync(int id, CancellationToken ct = default);
    Task<JobRequestDetail> SetRequestStatusAsync(int id, string status, CancellationToken ct = default);
    Task<JobRequestDetail> SaveDetailsAsync(int id, IEnumerable<DetailValue> values, CancellationToken ct = default);
    Task SetEvaluationDateAsync(int requestId, DateTime date, CancellationToken ct = default);
    Task<int> CreateRequestAsync(NewRequestInput input, CancellationToken ct = default);

    // ---- jobs
    Task<List<JobSummary>> GetJobsAsync(string? status, string? search, CancellationToken ct = default);
    Task<JobDetail> GetJobAsync(int id, CancellationToken ct = default);
    Task<JobDetail> SetJobStatusAsync(int id, string status, decimal? fuelCost, decimal? dumpingCost, CancellationToken ct = default);

    // ---- photos
    Task<List<SitePhoto>> GetPhotosAsync(PhotoTarget target, CancellationToken ct = default);
    Task<SitePhoto> UploadPhotoAsync(PhotoTarget target, string photoType, string filePath, CancellationToken ct = default);
    Task<SitePhoto> UpdatePhotoTypeAsync(int photoId, string photoType, CancellationToken ct = default);
    Task DeletePhotoAsync(int photoId, CancellationToken ct = default);
    /// <summary>Returns null when the server (or demo mode) has no image bytes for this photo.</summary>
    Task<byte[]?> DownloadPhotoAsync(int photoId, int? width, CancellationToken ct = default);

    // ---- lookups + calendar
    Task<LookupsDto> GetLookupsAsync(CancellationToken ct = default);
    Task<List<CalendarEvent>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // ---- clients
    Task<List<ClientSummary>> GetClientsAsync(string? search, CancellationToken ct = default);
    Task<ClientDetail> GetClientAsync(int id, CancellationToken ct = default);
    Task<ClientDetail> SaveClientAsync(int? id, ClientInput input, CancellationToken ct = default);

    // ---- quotes
    Task<List<QuoteSummary>> GetQuotesAsync(string? status, string? search, CancellationToken ct = default);
    Task<QuoteDetail> GetQuoteAsync(int id, CancellationToken ct = default);
    Task<QuotePreview> GetQuotePreviewAsync(int requestId, CancellationToken ct = default);
    Task<QuoteDetail> CreateQuoteAsync(int requestId, CancellationToken ct = default);
    Task<QuoteDetail> UpdateQuoteAsync(int id, string? status, decimal? amount, DateTime? expiry, CancellationToken ct = default);

    // ---- allocation
    Task<List<StaffAllocation>> GetStaffAllocationsAsync(string? search, CancellationToken ct = default);
    Task<List<AssetAllocation>> GetAssetAllocationsAsync(string? search, CancellationToken ct = default);
    Task<JobAllocations> GetJobAllocationsAsync(int jobId, CancellationToken ct = default);
    Task<AssignOptions> GetAssignOptionsAsync(int jobId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default);
    Task<JobAllocations> AssignStaffAsync(int jobId, int staffId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default);
    Task<JobAllocations> AssignAssetAsync(int jobId, string kind, int assetId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default);
    Task UnassignStaffAsync(int jobId, int staffId, CancellationToken ct = default);
    Task UnassignAssetAsync(int assignmentId, CancellationToken ct = default);

    // ---- payments + reports
    Task<List<PaymentJob>> GetPaymentsAsync(string? filter, string? search, CancellationToken ct = default);
    Task<JobPayments> GetJobPaymentsAsync(int jobId, CancellationToken ct = default);
    Task<JobPayments> RecordPaymentAsync(int jobId, decimal amount, string method, string? reference, CancellationToken ct = default);
    Task<ReportDto> GetReportAsync(CancellationToken ct = default);
}
