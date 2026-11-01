namespace GiantGroupOpsPlus.Services;

public static class StatusColors
{
    public static Color For(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "pending" => Color.FromArgb("#E08600"),
        "evaluation date set" => Color.FromArgb("#1565C0"),
        "site evaluated" => Color.FromArgb("#1F6B3A"),
        "quote sent" => Color.FromArgb("#6A4C93"),
        "quote accepted" => Color.FromArgb("#2E7D32"),
        "cancelled" => Color.FromArgb("#B23A3A"),
        "not started" => Color.FromArgb("#6B7A71"),
        "in progress" => Color.FromArgb("#E08600"),
        "completed" => Color.FromArgb("#2E7D32"),
        "before" => Color.FromArgb("#1565C0"),
        "after" => Color.FromArgb("#2E7D32"),
        "failed" => Color.FromArgb("#C62828"),
        _ => Color.FromArgb("#6B7A71")
    };
}
