using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class ClientEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int? _id;
    private bool _loaded;

    public ClientEditViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public string[] Types { get; } = { "Residential", "Commercial" };

    [ObservableProperty] private string _pageTitle = "New client";
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _surname = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _clientType = "Residential";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var v)) _id = Convert.ToInt32(v);
        PageTitle = _id.HasValue ? "Edit client" : "New client";
    }

    public async Task OnAppearingAsync()
    {
        if (_loaded || !_id.HasValue) return;
        _loaded = true;
        await RunAsync(async () =>
        {
            var c = await _api.GetClientAsync(_id.Value);
            FirstName = c.FirstName; Surname = c.Surname; Email = c.Email ?? ""; Phone = c.Phone ?? "";
            ClientType = string.IsNullOrWhiteSpace(c.ClientType) ? "Residential" : c.ClientType!;
        }, asLoad: true);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName)) { await _dialogs.AlertAsync("First name", "Enter the client's first name."); return; }
        if (string.IsNullOrWhiteSpace(Surname)) { await _dialogs.AlertAsync("Surname", "Enter the client's surname."); return; }
        var ok = await RunAsync(async () =>
        {
            await _api.SaveClientAsync(_id, new ClientInput
            {
                FirstName = FirstName.Trim(), Surname = Surname.Trim(), Email = Email.Trim(), Phone = Phone.Trim(), ClientType = ClientType
            });
        });
        if (ok) await Shell.Current.GoToAsync("..");
    }
}
