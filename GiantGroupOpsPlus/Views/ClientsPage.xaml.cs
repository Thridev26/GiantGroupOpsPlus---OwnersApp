using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _vm;

    public ClientsPage(ClientsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

    private void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ClientSummary c)
        {
            _vm.OpenCommand.Execute(c);
            if (sender is CollectionView cv) cv.SelectedItem = null;
        }
    }
}
