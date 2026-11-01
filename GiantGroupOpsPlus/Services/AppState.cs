namespace GiantGroupOpsPlus.Services;

/// <summary>Tiny shared state for hand-offs between tabs (e.g. tapping a dashboard tile pre-filters the Requests tab).</summary>
public class AppState
{
    public string? RequestFilter { get; set; }
    public string? JobFilter { get; set; }
}
