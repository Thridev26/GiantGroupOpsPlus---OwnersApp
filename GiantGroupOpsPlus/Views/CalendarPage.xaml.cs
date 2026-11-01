using GiantGroupOpsPlus.Models;
using GiantGroupOpsPlus.ViewModels;

namespace GiantGroupOpsPlus.Views;

public partial class CalendarPage : ContentPage
{
    private readonly CalendarViewModel _vm;

    public CalendarPage(CalendarViewModel vm)
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
        if (e.CurrentSelection.FirstOrDefault() is CalendarEvent ev)
        {
            _vm.OpenCommand.Execute(ev);
            if (sender is CollectionView cv) cv.SelectedItem = null;
        }
    }
}
