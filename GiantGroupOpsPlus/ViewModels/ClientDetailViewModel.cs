using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public partial class ClientDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialogs;
    private int _id;

    public ClientDetailViewModel(IApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<ClientRequestRef> Requests { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasDetail))] private ClientDetail? _detail;
    [ObservableProperty] private bool _hasRequests;

    public bool HasDetail => Detail != null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var v)) _id = Convert.ToInt32(v);
    }

    public Task OnAppearingAsync() => LoadAsync(showBusy: Detail == null);

    [RelayCommand] private Task RefreshAsync() => LoadAsync(false);
    [RelayCommand] private Task RetryAsync() => LoadAsync(true);

    private async Task LoadAsync(bool showBusy) =>
        await RunAsync(async () =>
        {
            var d = await _api.GetClientAsync(_id);
            Detail = d;
            Requests.Clear();
            foreach (var r in d.Requests) Requests.Add(r);
            HasRequests = Requests.Count > 0;
        }, showBusy, asLoad: true);

    [RelayCommand] private Task CallAsync() => ContactActions.CallAsync(Detail?.Phone, _dialogs);
    [RelayCommand] private Task EmailAsync() => ContactActions.EmailAsync(Detail?.Email, _dialogs);

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync("clientedit", new ShellNavigationQueryParameters { ["id"] = _id });

    [RelayCommand]
    private Task NewRequestAsync() => Shell.Current.GoToAsync("newrequest", new ShellNavigationQueryParameters { ["clientId"] = _id });

    [RelayCommand]
    private Task OpenRequestAsync(ClientRequestRef? r) =>
        r == null ? Task.CompletedTask : Shell.Current.GoToAsync("requestdetail", new ShellNavigationQueryParameters { ["id"] = r.JobRequestId });
}
