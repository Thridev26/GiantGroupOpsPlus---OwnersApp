using System.Globalization;
using GiantGroupOpsPlus.Models;

namespace GiantGroupOpsPlus.Services;

/// <summary>Demo data for the features that only the full app has (calendar, clients, quotes, allocation, payments, reports).</summary>
public partial class DemoApiClient
{
    private readonly List<ClientDetail> _clients = new();
    private readonly List<QuoteDetail> _quotes = new();
    private readonly List<(int StaffId, string Name, string Role, string Phone)> _staffPool = new()
    {
        (11, "Mandla Zulu", "Ground crew", "082 555 0111"), (12, "Ryan Govender", "Tree climber", "083 555 0112"),
        (13, "Pravin Naidoo", "Driver", "071 555 0113"), (14, "Sizwe Khumalo", "Ground crew", "084 555 0114")
    };
    private readonly List<(string Kind, int Id, string Name, string Ident, string Owner)> _assetPool = new()
    {
        ("Owned", 1, "Chainsaw", "CS-2201", "Giant Group (Owned)"), ("Owned", 2, "Wood chipper", "WC-0310", "Giant Group (Owned)"),
        ("Owned", 3, "Bakkie", "ND 412 889", "Giant Group (Owned)"), ("Hired", 7, "Cherry picker", "Hired Asset #7", "Durban Plant Hire")
    };
    private readonly List<TimeSlotDto> _slots = new();
    private readonly List<(int Job, int Staff, DateTime Date, List<int> Slots)> _staffAlloc = new();
    private readonly List<(int Id, int Job, string Kind, int Asset, DateTime Date, List<int> Slots)> _assetAlloc = new();
    private readonly List<(int Job, PaymentEntry Entry)> _payments = new();
    private int _nextAssignment = 900, _nextQuote = 700, _nextClient = 50, _nextRequest = 1100;

    private void SeedPlus()
    {
        for (var h = 7; h < 17; h++) _slots.Add(new TimeSlotDto { Id = h - 6, Label = $"{h:00}:00–{h + 1:00}:00" });

        ClientDetail C(int id, string first, string last, string type, string phone) => new()
        {
            ClientId = id, FirstName = first, Surname = last, FullName = (first + " " + last).Trim(), ClientType = type, Phone = phone,
            Email = (first + "." + last).ToLower().Replace(' ', '.') + "@example.com", Status = "Active", DateAdded = DateTime.Today.AddDays(-id)
        };
        _clients.AddRange(new[]
        {
            C(1, "Thandi", "Nkosi", "Residential", "082 555 0141"), C(2, "Pieter", "van Wyk", "Residential", "083 555 0140"),
            C(3, "Aisha", "Patel", "Residential", "071 555 0139"), C(4, "Greenview", "Body Corporate", "Commercial", "031 555 0138"),
            C(5, "Sipho", "Dlamini", "Residential", "084 555 0137")
        });

        QuoteDetail Q(int id, int req, string client, string addr, string status, decimal amount) => new()
        {
            QuoteId = id, RequestId = req, ClientName = client, SiteAddress = addr, Status = status, Amount = amount,
            DateIssued = DateTime.Today.AddDays(-4), ExpiryDate = DateTime.Today.AddDays(26), ClientPhone = "031 555 0100", ClientEmail = "client@example.com",
            Breakdown = Figures(amount), AllowedStatuses = new() { "Pending", "Sent", "Accepted", "Declined", "Rejected" }
        };
        _quotes.AddRange(new[]
        {
            Q(501, 1030, "Naidoo Residence", "9 Oak Lane, Durban North", "Accepted", 8420m),
            Q(500, 1038, "Greenview Body Corporate", "Greenview Estate, Westville", "Accepted", 15350m),
            Q(499, 1028, "Hilltop Primary School", "Hilltop Rd, Kloof", "Accepted", 4980m),
            Q(502, 1039, "Aisha Patel", "22 Palm Close, Umhlanga", "Sent", 3150m)
        });

        // payments: job 301 part-paid, 299 paid in full, 300 unpaid
        _payments.Add((299, new PaymentEntry { Amount = 4980m, Method = "EFT", Reference = "INV-299", Date = DateTime.Today.AddDays(-2) }));
        _payments.Add((301, new PaymentEntry { Amount = 5000m, Method = "PayFast", Reference = "PF-88123", Date = DateTime.Today.AddDays(-1) }));

        _staffAlloc.Add((301, 11, DateTime.Today, new() { 1, 2, 3, 4 }));
        _assetAlloc.Add((_nextAssignment++, 301, "Owned", 1, DateTime.Today, new() { 1, 2, 3, 4 }));
    }

    private static QuoteFigures Figures(decimal total)
    {
        var sub = Math.Round(total / 1.15m, 2);
        var travel = 850m;
        var services = sub - travel;
        return new QuoteFigures
        {
            Lines = new() { new QuoteLine { Name = "Services", Rate = services, Quantity = 1, LineTotal = services } },
            Services = services, Travel = travel, TravelNote = "Demo travel estimate.", OneWayKm = 18.4, Subtotal = sub, Vat = total - sub, Total = total
        };
    }

    // ---- overview / lookups
    public async Task<OverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            decimal invoiced = _quotes.Where(q => _jobs.Any(j => j.QuoteId == q.QuoteId)).Sum(q => q.Amount);
            return new OverviewDto
            {
                QuotesSent = _quotes.Count(q => q.Status == "Sent"), QuotesAccepted = _quotes.Count(q => q.Status == "Accepted"),
                ReadyToQuote = _requests.Count(r => r.Status == Statuses.SiteEvaluated),
                JobsWithoutCrew = _jobs.Count(j => j.Status != Statuses.Completed && !_staffAlloc.Any(a => a.Job == j.JobId)),
                Clients = _clients.Count, Outstanding = invoiced - _payments.Sum(p => p.Entry.Amount)
            };
        }
    }

    public async Task<LookupsDto> GetLookupsAsync(CancellationToken ct = default)
    {
        await Latency();
        return new LookupsDto
        {
            TimeSlots = _slots.ToList(),
            JobTypes = new()
            {
                new() { Id = 1, Name = "Tree Felling", Rate = 450m }, new() { Id = 2, Name = "Grass Cutting", Rate = 12m },
                new() { Id = 3, Name = "Hedge Trimming", Rate = 35m }, new() { Id = 4, Name = "Stump Removal", Rate = 600m },
                new() { Id = 5, Name = "Vegetation Clearance", Rate = 18m }
            },
            Urgencies = new() { "High", "Medium", "Low", "Not Specified" }, ClientTypes = new() { "Residential", "Commercial" },
            QuoteStatuses = new() { "Pending", "Sent", "Accepted", "Declined", "Rejected" }
        };
    }

    // ---- calendar
    public async Task<List<CalendarEvent>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var list = new List<CalendarEvent>();
            foreach (var j in _jobs.Where(j => j.StartDate.HasValue && j.StartDate.Value.Date >= from.Date && j.StartDate.Value.Date < to.Date))
                list.Add(new CalendarEvent
                {
                    Kind = "job", Id = j.JobId, RequestId = j.JobRequestId, Date = j.StartDate!.Value.Date, Status = j.Status, Address = j.SiteAddress,
                    Latitude = j.Latitude, Longitude = j.Longitude, ClientName = j.ClientName, ClientPhone = j.ClientPhone, Items = j.RequestedItems,
                    Crew = string.Join(", ", _staffAlloc.Where(a => a.Job == j.JobId).Select(a => _staffPool.First(s => s.StaffId == a.Staff).Name))
                });
            foreach (var r in _requests.Where(r => r.SiteEvaluationDate.HasValue && r.Status != Statuses.Cancelled
                                                   && r.SiteEvaluationDate.Value.Date >= from.Date && r.SiteEvaluationDate.Value.Date < to.Date))
                list.Add(new CalendarEvent
                {
                    Kind = "evaluation", Id = r.JobRequestId, RequestId = r.JobRequestId, Date = r.SiteEvaluationDate!.Value.Date, Status = r.Status,
                    Address = r.SiteAddress, Latitude = r.Latitude, Longitude = r.Longitude, Urgency = r.Urgency, ClientName = r.ClientName,
                    ClientPhone = r.ClientPhone, Items = r.RequestedItems
                });
            return list.OrderBy(e => e.Date).ThenBy(e => e.Kind).ToList();
        }
    }

    // ---- requests
    public async Task SetEvaluationDateAsync(int requestId, DateTime date, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var r = Find(requestId);
            if (r.Status == Statuses.Cancelled) throw new ApiException("That request was not found or has been cancelled.", 409);
            r.SiteEvaluationDate = date.Date;
            if (r.Status == Statuses.Pending) r.Status = Statuses.EvaluationDateSet;
        }
    }

    public async Task<int> CreateRequestAsync(NewRequestInput input, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var client = _clients.FirstOrDefault(c => c.ClientId == input.ClientId) ?? throw new ApiException("Choose the client this request is for.", 400);
            var types = input.JobTypeIds.Select(id => id switch { 1 => "Tree Felling", 2 => "Grass Cutting", 3 => "Hedge Trimming", 4 => "Stump Removal", _ => "Vegetation Clearance" }).ToList();
            var id = _nextRequest++;
            _requests.Insert(0, new JobRequestDetail
            {
                JobRequestId = id, ClientName = client.FullName, ClientPhone = client.Phone, ClientEmail = client.Email, SiteAddress = input.SiteAddress,
                Latitude = input.Latitude, Longitude = input.Longitude, Urgency = input.Urgency, Source = "Mobile App", Status = Statuses.Pending,
                DateReceived = DateTime.Today, RequestedItems = string.Join(", ", types.OrderBy(t => t)),
                Items = types.Select((t, i) => new RequestItem { RequestItemId = id * 10 + i, JobTypeName = t }).ToList(),
                AllowedStatuses = Statuses.AllRequest.ToList()
            });
            return id;
        }
    }

    // ---- clients
    public async Task<List<ClientSummary>> GetClientsAsync(string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
            return _clients.Where(c => string.IsNullOrWhiteSpace(search) || c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                        || (c.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) || (c.Phone ?? "").Contains(search))
                           .Select(c => (ClientSummary)ToDetail(c)).ToList();
    }

    public async Task<ClientDetail> GetClientAsync(int id, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) return ToDetail(_clients.FirstOrDefault(c => c.ClientId == id) ?? throw new ApiException("Client not found.", 404));
    }

    private ClientDetail ToDetail(ClientDetail c)
    {
        var reqs = _requests.Where(r => r.ClientName == c.FullName).Select(r => new ClientRequestRef { JobRequestId = r.JobRequestId, Status = r.Status, SiteAddress = r.SiteAddress, DateReceived = r.DateReceived }).ToList();
        return new ClientDetail
        {
            ClientId = c.ClientId, FirstName = c.FirstName, Surname = c.Surname, FullName = c.FullName, ClientType = c.ClientType, Email = c.Email,
            Phone = c.Phone, Status = c.Status, DateAdded = c.DateAdded, RequestCount = reqs.Count, Requests = reqs
        };
    }

    public async Task<ClientDetail> SaveClientAsync(int? id, ClientInput input, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.Surname)) throw new ApiException("Enter the client's first name and surname.", 400);
            ClientDetail c;
            if (id.HasValue) c = _clients.FirstOrDefault(x => x.ClientId == id.Value) ?? throw new ApiException("Client not found.", 404);
            else { c = new ClientDetail { ClientId = _nextClient++, Status = "Active", DateAdded = DateTime.Today }; _clients.Add(c); }
            c.FirstName = input.FirstName.Trim(); c.Surname = input.Surname.Trim(); c.FullName = (c.FirstName + " " + c.Surname).Trim();
            c.ClientType = input.ClientType; c.Email = input.Email; c.Phone = input.Phone;
            return ToDetail(c);
        }
    }

    // ---- quotes
    public async Task<List<QuoteSummary>> GetQuotesAsync(string? status, string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
            return _quotes.Where(q => string.IsNullOrEmpty(status) || q.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                          .Where(q => string.IsNullOrWhiteSpace(search) || q.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase) || q.QuoteId.ToString() == search.Trim())
                          .OrderByDescending(q => q.QuoteId).Cast<QuoteSummary>().ToList();
    }

    public async Task<QuoteDetail> GetQuoteAsync(int id, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) return _quotes.FirstOrDefault(q => q.QuoteId == id) ?? throw new ApiException("Quote not found.", 404);
    }

    public async Task<QuotePreview> GetQuotePreviewAsync(int requestId, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var r = Find(requestId);
            var existing = _quotes.FirstOrDefault(q => q.RequestId == requestId);
            var measured = r.Items.SelectMany(i => i.Details).Any(d => !string.IsNullOrWhiteSpace(d.Value));
            string? blocker = r.Status == Statuses.Cancelled ? "This request has been cancelled, so a quote cannot be created."
                : existing != null || (r.Status is Statuses.QuoteSent or Statuses.QuoteAccepted) ? "A quote for this request already exists."
                : !measured ? "Capture the measurements (quantities) for the requested services first." : null;
            var f = Figures(Math.Round((measured ? 4200m : 850m) * 1.15m, 2));
            return new QuotePreview
            {
                RequestId = requestId, ClientName = r.ClientName, SiteAddress = r.SiteAddress, RequestStatus = r.Status, CanCreate = blocker == null, Blocker = blocker,
                QuoteId = existing?.QuoteId, Lines = f.Lines, Services = f.Services, Travel = f.Travel, TravelNote = f.TravelNote, OneWayKm = f.OneWayKm,
                Subtotal = f.Subtotal, Vat = f.Vat, Total = f.Total
            };
        }
    }

    public async Task<QuoteDetail> CreateQuoteAsync(int requestId, CancellationToken ct = default)
    {
        var p = await GetQuotePreviewAsync(requestId, ct);
        if (!p.CanCreate) throw new ApiException(p.Blocker ?? "A quote cannot be created.", 409);
        lock (_lock)
        {
            var q = new QuoteDetail
            {
                QuoteId = _nextQuote++, RequestId = requestId, ClientName = p.ClientName ?? "", SiteAddress = p.SiteAddress, Status = "Sent", Amount = p.Total,
                DateIssued = DateTime.Today, ExpiryDate = DateTime.Today.AddMonths(1), Breakdown = Figures(p.Total),
                AllowedStatuses = new() { "Pending", "Sent", "Accepted", "Declined", "Rejected" }
            };
            _quotes.Add(q);
            Find(requestId).Status = Statuses.QuoteSent;
            return q;
        }
    }

    public async Task<QuoteDetail> UpdateQuoteAsync(int id, string? status, decimal? amount, DateTime? expiry, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var q = _quotes.FirstOrDefault(x => x.QuoteId == id) ?? throw new ApiException("Quote not found.", 404);
            if (amount.HasValue) { q.Amount = amount.Value; q.AmountEdited = Math.Abs(q.Amount - q.Breakdown.Total) > 0.009m; }
            if (expiry.HasValue) q.ExpiryDate = expiry.Value;
            if (!string.IsNullOrWhiteSpace(status))
            {
                q.Status = status;
                var req = _requests.FirstOrDefault(r => r.JobRequestId == q.RequestId);
                if (req != null) req.Status = status switch { "Accepted" => Statuses.QuoteAccepted, "Rejected" or "Declined" => Statuses.Cancelled, "Sent" => Statuses.QuoteSent, _ => req.Status };
            }
            return q;
        }
    }

    // ---- allocation
    private string SlotText(IEnumerable<int> ids) => string.Join(", ", _slots.Where(s => ids.Contains(s.Id)).Select(s => s.Label));

    public async Task<List<StaffAllocation>> GetStaffAllocationsAsync(string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
            return _staffAlloc.Select(a =>
            {
                var j = FindJob(a.Job);
                return new StaffAllocation { StaffId = a.Staff, JobId = a.Job, AssignmentDate = a.Date, JobStatus = j.Status, HoursWorked = a.Slots.Count,
                    StaffName = _staffPool.First(s => s.StaffId == a.Staff).Name, ClientName = j.ClientName, SiteAddress = j.SiteAddress, Slots = SlotText(a.Slots) };
            }).Where(a => string.IsNullOrWhiteSpace(search) || a.StaffName.Contains(search, StringComparison.OrdinalIgnoreCase) || (a.ClientName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
              .OrderByDescending(a => a.AssignmentDate).ToList();
    }

    public async Task<List<AssetAllocation>> GetAssetAllocationsAsync(string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
            return _assetAlloc.Select(a =>
            {
                var j = FindJob(a.Job); var asset = _assetPool.First(x => x.Kind == a.Kind && x.Id == a.Asset);
                return new AssetAllocation { AssignmentId = a.Id, JobId = a.Job, AssignmentDate = a.Date, AssetName = $"{asset.Name} ({asset.Ident})", AssetSource = a.Kind,
                    ClientName = j.ClientName, SiteAddress = j.SiteAddress, Slots = SlotText(a.Slots) };
            }).Where(a => string.IsNullOrWhiteSpace(search) || a.AssetName.Contains(search, StringComparison.OrdinalIgnoreCase) || (a.ClientName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
              .OrderByDescending(a => a.AssignmentDate).ToList();
    }

    public async Task<JobAllocations> GetJobAllocationsAsync(int jobId, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) return BuildAlloc(jobId);
    }

    private JobAllocations BuildAlloc(int jobId) => new()
    {
        JobId = jobId,
        Staff = _staffAlloc.Where(a => a.Job == jobId).Select(a => new JobStaffAlloc { StaffId = a.Staff, Name = _staffPool.First(s => s.StaffId == a.Staff).Name,
            AssignmentDate = a.Date, HoursWorked = a.Slots.Count, Slots = SlotText(a.Slots) }).ToList(),
        Assets = _assetAlloc.Where(a => a.Job == jobId).Select(a => { var x = _assetPool.First(p => p.Kind == a.Kind && p.Id == a.Asset);
            return new JobAssetAlloc { AssignmentId = a.Id, AssignmentDate = a.Date, Name = $"{x.Name} ({x.Ident})", Source = a.Kind, Slots = SlotText(a.Slots) }; }).ToList()
    };

    public async Task<AssignOptions> GetAssignOptionsAsync(int jobId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default)
    {
        await Latency();
        var slots = slotIds.ToList();
        lock (_lock)
        {
            return new AssignOptions
            {
                Staff = _staffPool.Select(s =>
                {
                    var clash = _staffAlloc.FirstOrDefault(a => a.Staff == s.StaffId && a.Date.Date == date.Date && a.Slots.Intersect(slots).Any());
                    return new StaffOption { StaffId = s.StaffId, Name = s.Name, Role = s.Role, Phone = s.Phone, OnThisJob = clash.Job == jobId && clash.Job != 0,
                        Busy = clash.Job == 0 ? null : clash.Job == jobId ? "Already on this job" : $"Busy on Job #{clash.Job} at {SlotText(clash.Slots.Intersect(slots))}" };
                }).ToList(),
                Assets = _assetPool.Select(x =>
                {
                    var clash = _assetAlloc.FirstOrDefault(a => a.Kind == x.Kind && a.Asset == x.Id && a.Date.Date == date.Date && a.Slots.Intersect(slots).Any());
                    return new AssetOption { Kind = x.Kind, AssetId = x.Id, Name = x.Name, Identifier = x.Ident, Owner = x.Owner, OnThisJob = clash.Job == jobId && clash.Job != 0,
                        Busy = clash.Job == 0 ? null : clash.Job == jobId ? "Already on this job" : $"In use on Job #{clash.Job} at {SlotText(clash.Slots.Intersect(slots))}" };
                }).ToList()
            };
        }
    }

    public async Task<JobAllocations> AssignStaffAsync(int jobId, int staffId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default)
    {
        await Latency();
        var slots = slotIds.Distinct().ToList();
        if (slots.Count == 0) throw new ApiException("Choose at least one valid time slot.", 409);
        lock (_lock)
        {
            FindJob(jobId);
            var clash = _staffAlloc.FirstOrDefault(a => a.Staff == staffId && a.Date.Date == date.Date && a.Slots.Intersect(slots).Any());
            if (clash.Job != 0) throw new ApiException($"Can't assign: busy on Job #{clash.Job} on {date:yyyy-MM-dd}. Pick someone else or different slots.", 409);
            _staffAlloc.RemoveAll(a => a.Staff == staffId && a.Job == jobId);
            _staffAlloc.Add((jobId, staffId, date.Date, slots));
            return BuildAlloc(jobId);
        }
    }

    public async Task<JobAllocations> AssignAssetAsync(int jobId, string kind, int assetId, DateTime date, IEnumerable<int> slotIds, CancellationToken ct = default)
    {
        await Latency();
        var slots = slotIds.Distinct().ToList();
        if (slots.Count == 0) throw new ApiException("Choose at least one valid time slot.", 409);
        lock (_lock)
        {
            FindJob(jobId);
            var clash = _assetAlloc.FirstOrDefault(a => a.Kind == kind && a.Asset == assetId && a.Date.Date == date.Date && a.Slots.Intersect(slots).Any());
            if (clash.Job != 0) throw new ApiException($"Can't assign: in use on Job #{clash.Job} on {date:yyyy-MM-dd}. Pick a different asset or different slots.", 409);
            _assetAlloc.Add((_nextAssignment++, jobId, kind, assetId, date.Date, slots));
            return BuildAlloc(jobId);
        }
    }

    public async Task UnassignStaffAsync(int jobId, int staffId, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) _staffAlloc.RemoveAll(a => a.Job == jobId && a.Staff == staffId);
    }

    public async Task UnassignAssetAsync(int assignmentId, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) _assetAlloc.RemoveAll(a => a.Id == assignmentId);
    }

    // ---- payments
    private PaymentJob PayRow(JobDetail j)
    {
        var total = _quotes.FirstOrDefault(q => q.QuoteId == j.QuoteId)?.Amount ?? 6000m;
        var paid = _payments.Where(p => p.Job == j.JobId).Sum(p => p.Entry.Amount);
        return new PaymentJob { JobId = j.JobId, RequestId = j.JobRequestId, Status = j.Status, ClientName = j.ClientName, SiteAddress = j.SiteAddress, InvoiceTotal = total, TotalPaid = paid, Balance = total - paid };
    }

    public async Task<List<PaymentJob>> GetPaymentsAsync(string? filter, string? search, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
            return _jobs.Select(PayRow)
                .Where(p => filter == "outstanding" ? !p.IsSettled : filter == "paid" ? p.IsSettled : true)
                .Where(p => string.IsNullOrWhiteSpace(search) || p.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase) || p.JobId.ToString() == search.Trim())
                .OrderByDescending(p => p.JobId).ToList();
    }

    public async Task<JobPayments> GetJobPaymentsAsync(int jobId, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock) return BuildPayments(jobId);
    }

    private JobPayments BuildPayments(int jobId)
    {
        var j = FindJob(jobId); var r = PayRow(j);
        return new JobPayments { JobId = r.JobId, RequestId = r.RequestId, Status = r.Status, ClientName = r.ClientName, ClientPhone = j.ClientPhone, SiteAddress = r.SiteAddress,
            InvoiceTotal = r.InvoiceTotal, TotalPaid = r.TotalPaid, Balance = r.Balance, Payments = _payments.Where(p => p.Job == jobId).Select(p => p.Entry).OrderByDescending(p => p.Date).ToList() };
    }

    public async Task<JobPayments> RecordPaymentAsync(int jobId, decimal amount, string method, string? reference, CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var cur = BuildPayments(jobId);
            if (amount <= 0) throw new ApiException("The amount must be more than zero.", 400);
            if (amount > cur.Balance + 0.009m) throw new ApiException($"That is more than the outstanding balance of R {cur.Balance.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"))}.", 409);
            _payments.Add((jobId, new PaymentEntry { Amount = amount, Method = method, Reference = reference, Date = DateTime.Now }));
            return BuildPayments(jobId);
        }
    }

    // ---- reports
    public async Task<ReportDto> GetReportAsync(CancellationToken ct = default)
    {
        await Latency();
        lock (_lock)
        {
            var invoiced = _jobs.Sum(j => PayRow(j).InvoiceTotal); var collected = _payments.Sum(p => p.Entry.Amount);
            var sent = _quotes.Count; var accepted = _quotes.Count(q => q.Status == "Accepted");
            return new ReportDto
            {
                RequestsByStatus = _requests.GroupBy(r => r.Status).Select(g => new LabelCount { Label = g.Key, Count = g.Count() }).ToList(),
                JobsByStatus = _jobs.GroupBy(j => j.Status).Select(g => new LabelCount { Label = g.Key, Count = g.Count() }).ToList(),
                QuotesByStatus = _quotes.GroupBy(q => q.Status).Select(g => new LabelCount { Label = g.Key, Count = g.Count(), Value = g.Sum(q => q.Amount) }).ToList(),
                Invoiced = invoiced, Collected = collected, Outstanding = invoiced - collected,
                CollectionRate = invoiced > 0 ? Math.Round(collected / invoiced * 100, 1) : 0, QuoteAcceptRate = sent > 0 ? Math.Round(accepted * 100m / sent, 1) : 0,
                Clients = _clients.Count, CollectedLast30Days = collected,
                PaymentsByMonth = new() { new MonthValue { Label = DateTime.Today.ToString("MMM yyyy", CultureInfo.InvariantCulture), Value = collected } }
            };
        }
    }
}
