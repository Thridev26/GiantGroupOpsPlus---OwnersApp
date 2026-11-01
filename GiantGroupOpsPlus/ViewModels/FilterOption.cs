using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GiantGroupOpsPlus.ViewModels;

/// <summary>A selectable filter chip. Owns its own tap command so lists need no relative bindings.</summary>
public partial class FilterOption : ObservableObject
{
    private readonly Action<FilterOption> _onSelected;

    public FilterOption(string label, string? value, Action<FilterOption> onSelected)
    {
        Label = label;
        Value = value;
        _onSelected = onSelected;
    }

    public string Label { get; }
    /// <summary>Null means "All".</summary>
    public string? Value { get; }

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => _onSelected(this);
}
