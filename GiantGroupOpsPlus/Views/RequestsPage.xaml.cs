using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class RequestsPage : ContentPage
{
    private readonly RequestsViewModel _vm;

    public RequestsPage(RequestsViewModel vm)
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
        if (e.CurrentSelection.FirstOrDefault() is JobRequestSummary r)
        {
            _vm.OpenCommand.Execute(r);
            if (sender is CollectionView cv) cv.SelectedItem = null;   // allow tapping the same row again
        }
    }
}
