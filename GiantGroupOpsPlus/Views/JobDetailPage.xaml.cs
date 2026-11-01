using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class JobDetailPage : ContentPage
{
    private readonly JobDetailViewModel _vm;

    public JobDetailPage(JobDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnDisappearing();
    }

    private void OnRemoveStaff(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is JobStaffAlloc s) _vm.RemoveStaffCommand.Execute(s);
    }

    private void OnRemoveAsset(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is JobAssetAlloc a) _vm.RemoveAssetCommand.Execute(a);
    }
}
