using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class AssignPage : ContentPage
{
    private readonly AssignViewModel _vm;

    public AssignPage(AssignViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

    private void OnStaffTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is StaffOption s) _vm.AssignStaffCommand.Execute(s);
    }

    private void OnAssetTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is AssetOption a) _vm.AssignAssetCommand.Execute(a);
    }
}
