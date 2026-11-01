using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class RequestDetailPage : ContentPage
{
    private readonly RequestDetailViewModel _vm;

    public RequestDetailPage(RequestDetailViewModel vm)
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

    private void OnJobClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is JobRef j) _vm.OpenJobCommand.Execute(j);
    }
}
