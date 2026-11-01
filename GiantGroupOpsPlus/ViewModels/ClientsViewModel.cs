using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class ClientsViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _searchCts;

    public ClientsViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ClientSummary> Clients { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _summary = "";

    public Task OnAppearingAsync() => LoadAsync(showBusy: Clients.Count == 0);

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        _ = Task.Delay(450, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync(false));
        });
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy)
    {
        await RunAsync(async () =>
        {
            var list = await _api.GetClientsAsync(SearchText);
            Clients.Clear();
            foreach (var c in list) Clients.Add(c);
            IsEmpty = Clients.Count == 0;
            Summary = Clients.Count == 1 ? "1 client" : $"{Clients.Count} clients";
        }, showBusy, asLoad: true);
    }

    [RelayCommand]
    private Task OpenAsync(ClientSummary? c) =>
        c == null ? Task.CompletedTask : Shell.Current.GoToAsync("clientdetail", new ShellNavigationQueryParameters { ["id"] = c.ClientId });

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync("clientedit");
}
