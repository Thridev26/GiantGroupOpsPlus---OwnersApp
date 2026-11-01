using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

/// <summary>A time slot the user can switch on/off. Owns its tap command so the list needs no relative bindings.</summary>
public partial class SlotChoice : ObservableObject
{
    private readonly Action _changed;
    public SlotChoice(TimeSlotDto s, Action changed) { Id = s.Id; Label = s.Label; _changed = changed; }
    public int Id { get; }
    public string Label { get; }
    [ObservableProperty] private bool _isSelected;

    [RelayCommand]
    private void Toggle() { IsSelected = !IsSelected; _changed(); }
}

/// <summary>Book staff or equipment onto a job for a date + time slots. Clashes come from the server (same rules as the website).</summary>
public partial class AssignViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int _jobId;
    private bool _loaded;
    private CancellationTokenSource? _debounce;

    public AssignViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<SlotChoice> Slots { get; } = new();
    public ObservableCollection<StaffOption> StaffOptions { get; } = new();
    public ObservableCollection<AssetOption> AssetOptions { get; } = new();

    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _title = "Assign";
    [ObservableProperty] private bool _isStaffMode = true;
    [ObservableProperty] private bool _hasSlots;
    [ObservableProperty] private string _hint = "Pick the time slots first.";

    public bool IsAssetMode => !IsStaffMode;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("jobId", out var j)) _jobId = Convert.ToInt32(j);
        if (query.TryGetValue("mode", out var m)) IsStaffMode = !string.Equals(Convert.ToString(m), "asset", StringComparison.OrdinalIgnoreCase);
        Title = IsStaffMode ? $"Assign staff · Job #{_jobId}" : $"Assign equipment · Job #{_jobId}";
        OnPropertyChanged(nameof(IsAssetMode));
    }

    public async Task OnAppearingAsync()
    {
        if (_loaded) return;
        await RunAsync(async () =>
        {
            var lookups = await _api.GetLookupsAsync();
            Slots.Clear();
            foreach (var s in lookups.TimeSlots) Slots.Add(new SlotChoice(s, OnSelectionChanged));
            HasSlots = Slots.Count > 0;
            _loaded = true;
            await ReloadAsync();
        }, asLoad: true);
    }

    [RelayCommand] private async Task RetryAsync() { _loaded = false; await OnAppearingAsync(); }

    partial void OnDateChanged(DateTime value) { if (_loaded) OnSelectionChanged(); }

    private void OnSelectionChanged()
    {
        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();
        _ = Task.Delay(300, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) MainThread.BeginInvokeOnMainThread(() => _ = ReloadAsync());
        });
    }

    private IEnumerable<int> SelectedSlots => Slots.Where(s => s.IsSelected).Select(s => s.Id);

    private async Task ReloadAsync()
    {
        var slots = SelectedSlots.ToList();
        Hint = slots.Count == 0 ? "Pick the time slots first, then you'll see who is free." : "Tap a name to assign.";
        await RunAsync(async () =>
        {
            var o = await _api.GetAssignOptionsAsync(_jobId, Date, slots);
            StaffOptions.Clear(); foreach (var s in o.Staff) StaffOptions.Add(s);
            AssetOptions.Clear(); foreach (var a in o.Assets) AssetOptions.Add(a);
        }, showBusy: false);
    }

    [RelayCommand]
    private async Task AssignStaffAsync(StaffOption? s)
    {
        if (s == null) return;
        var slots = SelectedSlots.ToList();
        if (slots.Count == 0) { await _dialogs.AlertAsync("Pick time slots", "Choose at least one time slot first."); return; }
        if (!s.IsFree && !s.OnThisJob) { await _dialogs.AlertAsync("Not available", s.Busy ?? "This person is busy for those slots."); return; }
        var when = $"{Date:ddd d MMM}, {string.Join(", ", Slots.Where(x => x.IsSelected).Select(x => x.Label))}";
        if (!await _dialogs.ConfirmAsync("Assign staff", $"Assign {s.Name} to Job #{_jobId}?\n{when}", "Assign")) return;
        var ok = await RunAsync(async () => await _api.AssignStaffAsync(_jobId, s.StaffId, Date, slots));
        if (ok) { ShowBanner($"{s.Name} assigned"); await ReloadAsync(); }
    }

    [RelayCommand]
    private async Task AssignAssetAsync(AssetOption? a)
    {
        if (a == null) return;
        var slots = SelectedSlots.ToList();
        if (slots.Count == 0) { await _dialogs.AlertAsync("Pick time slots", "Choose at least one time slot first."); return; }
        if (!a.IsFree && !a.OnThisJob) { await _dialogs.AlertAsync("Not available", a.Busy ?? "This equipment is in use for those slots."); return; }
        var when = $"{Date:ddd d MMM}, {string.Join(", ", Slots.Where(x => x.IsSelected).Select(x => x.Label))}";
        if (!await _dialogs.ConfirmAsync("Assign equipment", $"Assign {a.Name} to Job #{_jobId}?\n{when}", "Assign")) return;
        var ok = await RunAsync(async () => await _api.AssignAssetAsync(_jobId, a.Kind, a.AssetId, Date, slots));
        if (ok) { ShowBanner($"{a.Name} assigned"); await ReloadAsync(); }
    }
}
