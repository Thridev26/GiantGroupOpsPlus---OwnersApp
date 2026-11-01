using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace GiantGroupOpsPlus.Models;

// ------------------------------------------------------------------ shared lookups
public class TimeSlotDto
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}

public class JobTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
}

public class LookupsDto
{
    public List<TimeSlotDto> TimeSlots { get; set; } = new();
    public List<JobTypeDto> JobTypes { get; set; } = new();
    public List<string> Urgencies { get; set; } = new();
    public List<string> ClientTypes { get; set; } = new();
    public List<string> QuoteStatuses { get; set; } = new();
}

public class OverviewDto
{
    public int QuotesSent { get; set; }
    public int QuotesAccepted { get; set; }
    public int ReadyToQuote { get; set; }
    public int JobsWithoutCrew { get; set; }
    public int Clients { get; set; }
    public decimal Outstanding { get; set; }
}

// ------------------------------------------------------------------ calendar
public class CalendarEvent
{
    public string Kind { get; set; } = "job";          // "job" | "evaluation"
    public int Id { get; set; }
    public int? RequestId { get; set; }
    public DateTime Date { get; set; }
    public string? Status { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Urgency { get; set; }
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public string? Items { get; set; }
    public string? Crew { get; set; }

    [JsonIgnore] public bool IsJob => Kind == "job";
    [JsonIgnore] public string Title => IsJob ? $"Job #{Id} · {ClientName}" : $"Site evaluation · {ClientName}";
    [JsonIgnore] public string KindText => IsJob ? "JOB" : "EVALUATION";
    [JsonIgnore] public string ItemsText => string.IsNullOrWhiteSpace(Items) ? "No items listed" : Items!;
    [JsonIgnore] public string CrewText => IsJob ? (string.IsNullOrWhiteSpace(Crew) ? "No crew assigned" : "Crew: " + Crew) : "";
    [JsonIgnore] public bool HasCrewText => IsJob;
}

/// <summary>One day in the agenda list (a group of events).</summary>
public class CalendarDay : ObservableCollection<CalendarEvent>
{
    public CalendarDay(DateTime date, IEnumerable<CalendarEvent> events) : base(events)
    {
        Date = date;
        Title = date.Date == DateTime.Today ? "Today · " + date.ToString("ddd d MMM", CultureInfo.InvariantCulture)
              : date.Date == DateTime.Today.AddDays(1) ? "Tomorrow · " + date.ToString("ddd d MMM", CultureInfo.InvariantCulture)
              : date.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
    }
    public DateTime Date { get; }
    public string Title { get; }
}

// ------------------------------------------------------------------ clients
public class ClientSummary
{
    public int ClientId { get; set; }
    public string FirstName { get; set; } = "";
    public string Surname { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? ClientType { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Status { get; set; }
    public DateTime? DateAdded { get; set; }
    public int RequestCount { get; set; }

    [JsonIgnore] public string ContactText => string.Join("  ·  ", new[] { Phone, Email }.Where(x => !string.IsNullOrWhiteSpace(x)));
    [JsonIgnore] public string RequestCountText => RequestCount == 1 ? "1 request" : $"{RequestCount} requests";
    [JsonIgnore] public string TypeText => string.IsNullOrWhiteSpace(ClientType) ? "Client" : ClientType!;
    public override string ToString() => FullName;
}

public class ClientRequestRef
{
    public int JobRequestId { get; set; }
    public string? Status { get; set; }
    public string? SiteAddress { get; set; }
    public DateTime? DateReceived { get; set; }
    [JsonIgnore] public string Text => $"#{JobRequestId} · {SiteAddress}";
}

public class ClientDetail : ClientSummary
{
    public List<ClientRequestRef> Requests { get; set; } = new();
}

public class ClientInput
{
    public string FirstName { get; set; } = "";
    public string Surname { get; set; } = "";
    public string ClientType { get; set; } = "Residential";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class NewRequestInput
{
    public int ClientId { get; set; }
    public string SiteAddress { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Urgency { get; set; } = "Not Specified";
    public List<int> JobTypeIds { get; set; } = new();
}

// ------------------------------------------------------------------ quotes
public class QuoteLine
{
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal Quantity { get; set; }
    public decimal LineTotal { get; set; }
    [JsonIgnore] public string Detail => $"{Quantity:0.##} × {Money(Rate)}";
    [JsonIgnore] public string TotalText => Money(LineTotal);
    private static string Money(decimal v) => "R " + v.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"));
}

public class QuoteFigures
{
    public List<QuoteLine> Lines { get; set; } = new();
    public decimal Services { get; set; }
    public decimal Travel { get; set; }
    public string? TravelNote { get; set; }
    public double OneWayKm { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
}

public class QuotePreview : QuoteFigures
{
    public int RequestId { get; set; }
    public string? ClientName { get; set; }
    public string? SiteAddress { get; set; }
    public string? RequestStatus { get; set; }
    public bool CanCreate { get; set; }
    public string? Blocker { get; set; }
    public int? QuoteId { get; set; }
}

public class QuoteSummary
{
    public int QuoteId { get; set; }
    public int RequestId { get; set; }
    public string ClientName { get; set; } = "";
    public string? SiteAddress { get; set; }
    public DateTime? DateIssued { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";

    [JsonIgnore] public string Title => $"Quote #{QuoteId} · {ClientName}";
    [JsonIgnore] public string AmountText => "R " + Amount.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"));
    [JsonIgnore] public string ExpiryText => ExpiryDate.HasValue ? $"Expires {ExpiryDate:d MMM yyyy}" : "No expiry date";
}

public class QuoteDetail : QuoteSummary
{
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public QuoteFigures Breakdown { get; set; } = new();
    public bool AmountEdited { get; set; }
    public List<string> AllowedStatuses { get; set; } = new();
}

// ------------------------------------------------------------------ allocation
public class StaffAllocation
{
    public int StaffId { get; set; }
    public int JobId { get; set; }
    public DateTime? AssignmentDate { get; set; }
    public string? JobStatus { get; set; }
    public decimal? HoursWorked { get; set; }
    public string StaffName { get; set; } = "";
    public string? ClientName { get; set; }
    public string? SiteAddress { get; set; }
    public string? Slots { get; set; }

    [JsonIgnore] public string WhenText => (AssignmentDate.HasValue ? AssignmentDate.Value.ToString("ddd d MMM yyyy") : "No date") + (string.IsNullOrWhiteSpace(Slots) ? "" : "  ·  " + Slots);
    [JsonIgnore] public string JobText => $"Job #{JobId} · {ClientName}";
}

public class AssetAllocation
{
    public int AssignmentId { get; set; }
    public int JobId { get; set; }
    public DateTime? AssignmentDate { get; set; }
    public string AssetName { get; set; } = "";
    public string? AssetSource { get; set; }
    public string? ClientName { get; set; }
    public string? SiteAddress { get; set; }
    public string? Slots { get; set; }

    [JsonIgnore] public string WhenText => (AssignmentDate.HasValue ? AssignmentDate.Value.ToString("ddd d MMM yyyy") : "No date") + (string.IsNullOrWhiteSpace(Slots) ? "" : "  ·  " + Slots);
    [JsonIgnore] public string JobText => $"Job #{JobId} · {ClientName}";
}

public class JobStaffAlloc
{
    public int StaffId { get; set; }
    public string Name { get; set; } = "";
    public DateTime? AssignmentDate { get; set; }
    public decimal? HoursWorked { get; set; }
    public string? Slots { get; set; }
    [JsonIgnore] public string WhenText => (AssignmentDate.HasValue ? AssignmentDate.Value.ToString("ddd d MMM") : "") + (string.IsNullOrWhiteSpace(Slots) ? "" : "  ·  " + Slots);
}

public class JobAssetAlloc
{
    public int AssignmentId { get; set; }
    public DateTime? AssignmentDate { get; set; }
    public string Name { get; set; } = "";
    public string? Source { get; set; }
    public string? Slots { get; set; }
    [JsonIgnore] public string WhenText => (AssignmentDate.HasValue ? AssignmentDate.Value.ToString("ddd d MMM") : "") + (string.IsNullOrWhiteSpace(Slots) ? "" : "  ·  " + Slots);
    [JsonIgnore] public string NameText => string.IsNullOrWhiteSpace(Source) ? Name : $"{Name} ({Source})";
}

public class JobAllocations
{
    public int JobId { get; set; }
    public List<JobStaffAlloc> Staff { get; set; } = new();
    public List<JobAssetAlloc> Assets { get; set; } = new();
}

public class StaffOption
{
    public int StaffId { get; set; }
    public string Name { get; set; } = "";
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? Busy { get; set; }
    public bool OnThisJob { get; set; }

    [JsonIgnore] public bool IsFree => string.IsNullOrEmpty(Busy);
    [JsonIgnore] public string StatusText => IsFree ? "Free for these slots" : Busy!;
    [JsonIgnore] public string SubText => string.Join("  ·  ", new[] { Role, Phone }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public class AssetOption
{
    public string Kind { get; set; } = "Owned";
    public int AssetId { get; set; }
    public string Name { get; set; } = "";
    public string? Identifier { get; set; }
    public string? Owner { get; set; }
    public string? Busy { get; set; }
    public bool OnThisJob { get; set; }

    [JsonIgnore] public bool IsFree => string.IsNullOrEmpty(Busy);
    [JsonIgnore] public string StatusText => IsFree ? "Free for these slots" : Busy!;
    [JsonIgnore] public string SubText => string.Join("  ·  ", new[] { Kind, Identifier, Owner }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public class AssignOptions
{
    public List<StaffOption> Staff { get; set; } = new();
    public List<AssetOption> Assets { get; set; } = new();
}

// ------------------------------------------------------------------ payments
public class PaymentJob
{
    public int JobId { get; set; }
    public int? RequestId { get; set; }
    public string? Status { get; set; }
    public string ClientName { get; set; } = "";
    public string? SiteAddress { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    [JsonIgnore] public string Title => $"Job #{JobId} · {ClientName}";
    [JsonIgnore] public bool IsSettled => Balance <= 0.009m;
    [JsonIgnore] public string BalanceText => IsSettled ? "Paid in full" : "Owing " + Fmt(Balance);
    [JsonIgnore] public string InvoiceText => "Invoice " + Fmt(InvoiceTotal);
    [JsonIgnore] public string PaidText => "Paid " + Fmt(TotalPaid);
    [JsonIgnore] public string BalanceStatus => IsSettled ? "Completed" : (TotalPaid > 0 ? "In Progress" : "Pending");
    private static string Fmt(decimal v) => "R " + v.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"));
}

public class PaymentEntry
{
    public decimal Amount { get; set; }
    public string? Method { get; set; }
    public string? Reference { get; set; }
    public DateTime? Date { get; set; }

    [JsonIgnore] public string AmountText => "R " + Amount.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"));
    [JsonIgnore] public string DetailText => string.Join("  ·  ", new[] { Method, Reference, Date?.ToString("d MMM yyyy") }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public class JobPayments : PaymentJob
{
    public string? ClientPhone { get; set; }
    public List<PaymentEntry> Payments { get; set; } = new();
}

// ------------------------------------------------------------------ reports
public class LabelCount
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class MonthValue
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
}

public class ReportDto
{
    public List<LabelCount> RequestsByStatus { get; set; } = new();
    public List<LabelCount> JobsByStatus { get; set; } = new();
    public List<LabelCount> QuotesByStatus { get; set; } = new();
    public decimal Invoiced { get; set; }
    public decimal Collected { get; set; }
    public decimal Outstanding { get; set; }
    public decimal CollectionRate { get; set; }
    public decimal QuoteAcceptRate { get; set; }
    public int Clients { get; set; }
    public decimal? CollectedLast30Days { get; set; }
    public List<MonthValue> PaymentsByMonth { get; set; } = new();
}
