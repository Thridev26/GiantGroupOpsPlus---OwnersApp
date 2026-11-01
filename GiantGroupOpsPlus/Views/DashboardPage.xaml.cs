using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
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
        if ((sender as BindableObject)?.BindingContext is JobRequestSummary r) _vm.OpenRequestCommand.Execute(r);
    }

    private void OnJobTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is JobSummary j) _vm.OpenJobCommand.Execute(j);
    }
}
