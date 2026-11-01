using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class PaymentsPage : ContentPage
{
    private readonly PaymentsViewModel _vm;

    public PaymentsPage(PaymentsViewModel vm)
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
        if (e.CurrentSelection.FirstOrDefault() is PaymentJob j)
        {
            _vm.OpenCommand.Execute(j);
            if (sender is CollectionView cv) cv.SelectedItem = null;
        }
    }
}
