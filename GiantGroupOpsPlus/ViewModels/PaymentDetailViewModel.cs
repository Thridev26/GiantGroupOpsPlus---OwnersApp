using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class PaymentDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int _jobId;
    private string _method = "EFT";

    public PaymentDetailViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
        foreach (var m in new[] { "Cash", "EFT", "Card", "Other" })
            Methods.Add(new FilterOption(m, m, OnMethod) { IsSelected = m == _method });
    }

    public ObservableCollection<FilterOption> Methods { get; } = new();
    public ObservableCollection<PaymentEntry> History { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasDetail))] private JobPayments? _detail;
    [ObservableProperty] private string _invoiceText = "";
    [ObservableProperty] private string _paidText = "";
    [ObservableProperty] private string _balanceText = "";
    [ObservableProperty] private bool _isSettled;
    [ObservableProperty] private bool _hasHistory;
    [ObservableProperty] private string _amountText = "";
    [ObservableProperty] private string _referenceText = "";

    public bool HasDetail => Detail != null;
    public bool CanRecord => !IsSettled;
    partial void OnIsSettledChanged(bool value) => OnPropertyChanged(nameof(CanRecord));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("jobId", out var v)) _jobId = Convert.ToInt32(v);
    }

    public Task OnAppearingAsync() => LoadAsync(showBusy: Detail == null);

    private void OnMethod(FilterOption o)
    {
        _method = o.Value ?? "EFT";
        foreach (var m in Methods) m.IsSelected = m == o;
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy) =>
        await RunAsync(async () => Apply(await _api.GetJobPaymentsAsync(_jobId)), showBusy, asLoad: true);

    private void Apply(JobPayments d)
    {
        Detail = d;
        InvoiceText = Money(d.InvoiceTotal); PaidText = Money(d.TotalPaid);
        BalanceText = d.IsSettled ? "Paid in full" : Money(d.Balance);
        IsSettled = d.IsSettled;
        History.Clear(); foreach (var p in d.Payments) History.Add(p);
        HasHistory = History.Count > 0;
    }

    [RelayCommand]
    private void PayFullBalance()
    {
        if (Detail != null && !Detail.IsSettled) AmountText = Detail.Balance.ToString("0.00", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private async Task RecordAsync()
    {
        if (Detail == null) return;
        if (!decimal.TryParse((AmountText ?? "").Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            await _dialogs.AlertAsync("Check the amount", "Enter the amount received, e.g. 1500.00");
            return;
        }
        if (amount > Detail.Balance + 0.009m)
        {
            await _dialogs.AlertAsync("Too much", $"The outstanding balance is only {Money(Detail.Balance)}.");
            return;
        }
        if (!await _dialogs.ConfirmAsync("Record payment", $"Record {Money(amount)} ({_method}) against job #{_jobId}?", "Record")) return;
        var reference = string.IsNullOrWhiteSpace(ReferenceText) ? null : ReferenceText.Trim();
        var ok = await RunAsync(async () =>
        {
            Apply(await _api.RecordPaymentAsync(_jobId, amount, _method, reference));
            AmountText = ""; ReferenceText = "";
            ShowBanner("Payment recorded");
        });
    }

    [RelayCommand] private Task CallAsync() => ContactActions.CallAsync(Detail?.ClientPhone, _dialogs);

    [RelayCommand]
    private Task OpenJobAsync() => Shell.Current.GoToAsync("jobdetail", new ShellNavigationQueryParameters { ["id"] = _jobId });
}
