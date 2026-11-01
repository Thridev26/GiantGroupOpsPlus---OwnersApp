using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class PhotoViewerPage : ContentPage
{
    public PhotoViewerPage(PhotoViewerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
