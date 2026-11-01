using System.Text.Json.Serialization;

namespace GiantGroupOpsPlus.Models;

public static class Statuses
{
    // Same vocabulary as the desktop app and website.
    public const string Pending = "Pending";
    public const string EvaluationDateSet = "Evaluation Date Set";
    public const string SiteEvaluated = "Site Evaluated";
    public const string QuoteSent = "Quote Sent";
    public const string QuoteAccepted = "Quote Accepted";
    public const string Cancelled = "Cancelled";

    public const string NotStarted = "Not Started";
    public const string InProgress = "In Progress";
    public const string Completed = "Completed";

    public static readonly string[] AllRequest = { Pending, EvaluationDateSet, SiteEvaluated, QuoteSent, QuoteAccepted, Cancelled };
    public static readonly string[] AllJob = { NotStarted, InProgress, Completed };
}

public enum PhotoTargetKind { Request, Job }

/// <summary>Identifies what a photo belongs to: a job request (site evaluation) or a job (work in progress).</summary>
public sealed record PhotoTarget(PhotoTargetKind Kind, int Id)
{
    public string Label => Kind == PhotoTargetKind.Job ? $"Job #{Id}" : $"Request #{Id}";
}

public class JobRequestSummary
{
    public int JobRequestId { get; set; }
    public string ClientName { get; set; } = "";
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public string SiteAddress { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Urgency { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "";
    public DateTime? DateReceived { get; set; }
    public DateTime? SiteEvaluationDate { get; set; }
    public string? RequestedItems { get; set; }
    public int PhotoCount { get; set; }

    [JsonIgnore] public string Title => $"#{JobRequestId} · {ClientName}";
    [JsonIgnore] public string ItemsText => string.IsNullOrWhiteSpace(RequestedItems) ? "No items listed" : RequestedItems!;
    [JsonIgnore] public string EvaluationText => SiteEvaluationDate.HasValue ? $"Evaluation {SiteEvaluationDate:ddd d MMM}" : "No evaluation date";
    [JsonIgnore] public string PhotoText => PhotoCount == 1 ? "1 photo" : $"{PhotoCount} photos";
    [JsonIgnore] public bool HasMap => Latitude.HasValue && Longitude.HasValue;
}

public class JobRef
{
    public int JobId { get; set; }
    public string Status { get; set; } = "";
    [JsonIgnore] public string Text => $"Job #{JobId} · {Status}";
}

public class DetailField
{
    public int JobDetailId { get; set; }
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "varchar";
    public string? Value { get; set; }
}

public class RequestItem
{
    public int RequestItemId { get; set; }
    public string JobTypeName { get; set; } = "";
    public List<DetailField> Details { get; set; } = new();
}

public class JobRequestDetail : JobRequestSummary
{
    public List<RequestItem> Items { get; set; } = new();
    public List<JobRef> Jobs { get; set; } = new();
    public List<string> AllowedStatuses { get; set; } = new();
}

public class DetailValue
{
    [JsonPropertyName("requestItemID")] public int RequestItemId { get; set; }
    [JsonPropertyName("jobDetailID")] public int JobDetailId { get; set; }
    [JsonPropertyName("value")] public string Value { get; set; } = "";
}

public class JobSummary
{
    public int JobId { get; set; }
    public int JobRequestId { get; set; }
    public string ClientName { get; set; } = "";
    public string? ClientPhone { get; set; }
    public string SiteAddress { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal FuelCost { get; set; }
    public decimal DumpingCost { get; set; }
    public decimal LabourCost { get; set; }
    public int? QuoteId { get; set; }
    public string? RequestedItems { get; set; }
    public int PhotoCount { get; set; }

    [JsonIgnore] public string Title => $"Job #{JobId} · {ClientName}";
    [JsonIgnore] public string ItemsText => string.IsNullOrWhiteSpace(RequestedItems) ? "No items listed" : RequestedItems!;
    [JsonIgnore] public string StartText => StartDate.HasValue ? $"Starts {StartDate:ddd d MMM}" : "No start date";
    [JsonIgnore] public string PhotoText => PhotoCount == 1 ? "1 photo" : $"{PhotoCount} photos";
    [JsonIgnore] public bool HasMap => Latitude.HasValue && Longitude.HasValue;
}

public class StaffAssignment
{
    public string Name { get; set; } = "";
    public decimal? HoursWorked { get; set; }
    [JsonIgnore] public string HoursText => HoursWorked.HasValue ? $"{HoursWorked:0.#} h" : "—";
}

public class JobDetail : JobSummary
{
    public List<StaffAssignment> Staff { get; set; } = new();
    public List<string> AllowedStatuses { get; set; } = new();
}

public class DashboardDto
{
    public int Pending { get; set; }
    public int ToEvaluate { get; set; }
    public int Evaluated { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int CompletedWeek { get; set; }
    public List<JobRequestSummary> UpcomingEvaluations { get; set; } = new();
    public List<JobSummary> ActiveJobs { get; set; } = new();
}

public class SitePhoto
{
    public int PhotoId { get; set; }
    public string PhotoType { get; set; } = "BEFORE";
    public DateTime UploadDate { get; set; }
    public int? JobId { get; set; }
    public int JobRequestId { get; set; }
}

public enum UploadState { Pending, Uploading, Failed }

/// <summary>A photo that has been captured and is waiting to be uploaded (persisted so it survives app restarts).</summary>
public class QueuedPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PhotoTargetKind Kind { get; set; }
    public int TargetId { get; set; }
    public string PhotoType { get; set; } = "BEFORE";
    public string LocalPath { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public int Attempts { get; set; }
    public UploadState State { get; set; }
    public string? LastError { get; set; }

    [JsonIgnore] public PhotoTarget Target => new(Kind, TargetId);
}
