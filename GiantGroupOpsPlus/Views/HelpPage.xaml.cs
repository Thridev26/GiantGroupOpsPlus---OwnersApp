using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class HelpPage : ContentPage
{
    public HelpPage(HelpViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
