namespace GiantGroupOpsPlus.Services;

public interface IDialogService
{
    Task AlertAsync(string title, string message, string ok = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel");
    Task<string?> ChooseAsync(string title, IEnumerable<string> options, string cancel = "Cancel", string? destructive = null);
    Task<string?> PromptAsync(string title, string message, string? initial = null, string placeholder = "", Keyboard? keyboard = null);
}

public class DialogService : IDialogService
{
    private static Page? Page => Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public Task AlertAsync(string title, string message, string ok = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => Page?.DisplayAlert(title, message, ok) ?? Task.CompletedTask);

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel") =>
        MainThread.InvokeOnMainThreadAsync<bool>(() => Page?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false));

    public Task<string?> ChooseAsync(string title, IEnumerable<string> options, string cancel = "Cancel", string? destructive = null) =>
        MainThread.InvokeOnMainThreadAsync<string?>(async () =>
        {
            if (Page == null) return null;
            var choice = await Page.DisplayActionSheet(title, cancel, destructive, options.ToArray());
            return choice == cancel ? null : choice;
        });

    public Task<string?> PromptAsync(string title, string message, string? initial = null, string placeholder = "", Keyboard? keyboard = null) =>
        MainThread.InvokeOnMainThreadAsync<string?>(async () =>
        {
            if (Page == null) return null;
            return await Page.DisplayPromptAsync(title, message, "Save", "Cancel", placeholder, -1, keyboard ?? Keyboard.Default, initial ?? "");
        });
}
