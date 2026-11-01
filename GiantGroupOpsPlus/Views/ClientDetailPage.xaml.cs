using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class ClientDetailPage : ContentPage
{
    private readonly ClientDetailViewModel _vm;

    public ClientDetailPage(ClientDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

    private void OnRequestTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is ClientRequestRef r) _vm.OpenRequestCommand.Execute(r);
    }
}
