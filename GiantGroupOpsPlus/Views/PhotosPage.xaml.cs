using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class PhotosPage : ContentPage
{
    private readonly PhotosViewModel _vm;

    public PhotosPage(PhotosViewModel vm)
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

    private void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PhotoItem item)
        {
            _vm.OpenCommand.Execute(item);
            if (sender is CollectionView cv) cv.SelectedItem = null;
        }
    }
}
