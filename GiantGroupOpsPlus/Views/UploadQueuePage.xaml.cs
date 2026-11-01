using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class UploadQueuePage : ContentPage
{
    private readonly UploadQueueViewModel _vm;

    public UploadQueuePage(UploadQueueViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnDisappearing();
    }
}
