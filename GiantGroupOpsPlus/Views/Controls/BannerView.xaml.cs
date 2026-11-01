using GiantGroupOpsPlus.Converters;

namespace GiantGroupOpsPlus.Views.Controls;

public partial class BannerView : ContentView
{
    public BannerView()
    {
        // Converter registered here so the control works on any page without page-level resources.
        Resources.Add("BannerColor", new BoolToColorConverter { TrueColor = Color.FromArgb("#C62828"), FalseColor = Color.FromArgb("#1F6B3A") });
        InitializeComponent();
    }
}
