using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class QuoteCreatePage : ContentPage
{
    private readonly QuoteCreateViewModel _vm;

    public QuoteCreatePage(QuoteCreateViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }
}
