using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class ServiceChoice : ObservableObject
{
    public ServiceChoice(JobTypeDto t) { Id = t.Id; Name = t.Name; }
    public int Id { get; }
    public string Name { get; }
    [ObservableProperty] private bool _isSelected;
}

/// <summary>Capture a job request on behalf of a client (phone call, walk-in, ...), with GPS or address lookup.</summary>
public partial class NewRequestViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int _presetClientId;
    private bool _loaded;
    private double? _lat, _lng;

    public NewRequestViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<ClientSummary> Clients { get; } = new();
    public ObservableCollection<ServiceChoice> Services { get; } = new();
    public ObservableCollection<string> Urgencies { get; } = new();

    [ObservableProperty] private ClientSummary? _selectedClient;
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _coordinatesText = "";
    [ObservableProperty] private string _locationStatus = "No location pinned yet.";
    [ObservableProperty] private string _urgency = "Not Specified";
    [ObservableProperty] private bool _clientLocked;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("clientId", out var v)) _presetClientId = Convert.ToInt32(v);
    }

    public async Task OnAppearingAsync()
    {
        if (_loaded) return;
        await RunAsync(async () =>
        {
            var lookups = await _api.GetLookupsAsync();
            var clients = await _api.GetClientsAsync(null);
            Clients.Clear(); foreach (var c in clients) Clients.Add(c);
            Services.Clear(); foreach (var t in lookups.JobTypes) Services.Add(new ServiceChoice(t));
            Urgencies.Clear(); foreach (var u in lookups.Urgencies) Urgencies.Add(u);
            if (_presetClientId > 0) { SelectedClient = Clients.FirstOrDefault(c => c.ClientId == _presetClientId); ClientLocked = SelectedClient != null; }
            _loaded = true;
        }, asLoad: true);
    }

    [RelayCommand] private async Task RetryAsync() { _loaded = false; await OnAppearingAsync(); }

    private void SetPin(double lat, double lng, string source)
    {
        _lat = lat; _lng = lng;
        CoordinatesText = string.Create(CultureInfo.InvariantCulture, $"{lat:0.000000}, {lng:0.000000}");
        LocationStatus = $"Pinned from {source}.";
    }

    [RelayCommand]
    private async Task UseMyLocationAsync()
    {
        await RunAsync(async () =>
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) { ShowBanner("Allow location access in your phone's settings to use this.", true); return; }
            var loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(12)));
            if (loc == null) { ShowBanner("Couldn't get your location. Try again outside or type the address.", true); return; }
            SetPin(loc.Latitude, loc.Longitude, "your current location");
            try
            {
                var marks = await Geocoding.Default.GetPlacemarksAsync(loc.Latitude, loc.Longitude);
                var p = marks?.FirstOrDefault();
                if (p != null && string.IsNullOrWhiteSpace(Address))
                    Address = string.Join(", ", new[] { $"{p.SubThoroughfare} {p.Thoroughfare}".Trim(), p.SubLocality ?? p.Locality }.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            catch (Exception) { /* address text is optional – the pin is what matters */ }
        });
    }

    [RelayCommand]
    private async Task FindAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(Address)) { await _dialogs.AlertAsync("Address needed", "Type the site address first."); return; }
        await RunAsync(async () =>
        {
            try
            {
                var found = (await Geocoding.Default.GetLocationsAsync(Address.Trim()))?.FirstOrDefault();
                if (found == null) { ShowBanner("Couldn't find that address. Add the suburb, or use your location.", true); return; }
                SetPin(found.Latitude, found.Longitude, "the address");
            }
            catch (Exception)
            {
                ShowBanner("Address lookup isn't available right now. Use your location or paste coordinates.", true);
            }
        });
    }

    partial void OnCoordinatesTextChanged(string value)
    {
        // Allows pasting "-29.85, 31.02" straight from a maps app.
        var parts = (value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var la)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lo) && Math.Abs(la) <= 90 && Math.Abs(lo) <= 180)
        {
            _lat = la; _lng = lo;
            LocationStatus = "Location pinned.";
        }
        else if (!string.IsNullOrWhiteSpace(value)) { _lat = _lng = null; LocationStatus = "Enter coordinates as: latitude, longitude"; }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (SelectedClient == null) { await _dialogs.AlertAsync("Choose a client", "Pick the client this request is for."); return; }
        if (string.IsNullOrWhiteSpace(Address) || Address.Trim().Length > 100) { await _dialogs.AlertAsync("Site address", "Enter a site address (max 100 characters)."); return; }
        if (_lat == null || _lng == null) { await _dialogs.AlertAsync("Pin the location", "Use your location, find the address, or paste coordinates so the team can find the site."); return; }
        var ids = Services.Where(s => s.IsSelected).Select(s => s.Id).ToList();
        if (ids.Count == 0) { await _dialogs.AlertAsync("Services", "Choose at least one service."); return; }

        int newId = 0;
        var ok = await RunAsync(async () =>
        {
            newId = await _api.CreateRequestAsync(new NewRequestInput
            {
                ClientId = SelectedClient.ClientId, SiteAddress = Address.Trim(), Latitude = _lat.Value, Longitude = _lng.Value,
                Urgency = Urgency, JobTypeIds = ids
            });
        });
        if (!ok) return;
        await _dialogs.AlertAsync("Request submitted", newId > 0 ? $"Job request #{newId} was created." : "The job request was created.");
        await Shell.Current.GoToAsync("..");
        if (newId > 0) await Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = newId });
    }
}
