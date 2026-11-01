using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class QuotesPage : ContentPage
{
    private readonly QuotesViewModel _vm;

    public QuotesPage(QuotesViewModel vm)
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
        if (e.CurrentSelection.FirstOrDefault() is QuoteSummary q)
        {
            _vm.OpenCommand.Execute(q);
            if (sender is CollectionView cv) cv.SelectedItem = null;
        }
    }
}
