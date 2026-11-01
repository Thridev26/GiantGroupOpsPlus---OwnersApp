using GiantGroupOpsPlus.Services;
using Microsoft.Maui.Controls.Shapes;

namespace GiantGroupOpsPlus.Views.Controls;

/// <summary>Small coloured pill showing a status ("In Progress", "BEFORE", ...). Colour is derived from the text.</summary>
public class StatusChip : Border
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(string), typeof(StatusChip), null,
        propertyChanged: (b, _, _) => ((StatusChip)b).Refresh());

    private readonly Label _label;

    public StatusChip()
    {
        Padding = new Thickness(10, 3);
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) };
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _label = new Label { TextColor = Colors.White, FontSize = 11.5, FontFamily = "OpenSansSemibold", LineBreakMode = LineBreakMode.NoWrap };
        Content = _label;
        Refresh();
    }

    public string? Status
    {
        get => (string?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    private void Refresh()
    {
        _label.Text = Status ?? "";
        BackgroundColor = StatusColors.For(Status);
        IsVisible = !string.IsNullOrEmpty(Status);
    }
}
