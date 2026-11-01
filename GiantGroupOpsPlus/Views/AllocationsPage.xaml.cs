using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class AllocationsPage : ContentPage
{
    private readonly AllocationsViewModel _vm;

    public AllocationsPage(AllocationsViewModel vm)
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
        if ((sender as BindableObject)?.BindingContext is StaffAllocation s) _vm.OpenJobCommand.Execute(s.JobId);
    }

    private void OnAssetTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is AssetAllocation a) _vm.OpenJobCommand.Execute(a.JobId);
    }
}
