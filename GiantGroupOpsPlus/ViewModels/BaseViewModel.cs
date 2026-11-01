using CommunityToolkit.Mvvm.ComponentModel;
using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    private int _bannerVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBanner))]
    private string? _bannerText;

    [ObservableProperty]
    private bool _bannerIsError;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasBanner => !string.IsNullOrEmpty(BannerText);

    /// <summary>Shows a message at the top of the page that clears itself.</summary>
    protected void ShowBanner(string text, bool isError = false)
    {
        var version = ++_bannerVersion;
        BannerText = text;
        BannerIsError = isError;
        _ = Task.Delay(isError ? 6000 : 3500).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() => { if (version == _bannerVersion) BannerText = null; }));
    }

    /// <summary>
    /// Runs work with consistent busy/error handling. Load failures (asLoad) fill ErrorMessage so the page can offer
    /// "Try again"; action failures show a banner. Auth failures are handled globally (session expiry → login).
    /// </summary>
    protected async Task<bool> RunAsync(Func<Task> work, bool showBusy = true, bool asLoad = false)
    {
        if (showBusy) IsBusy = true;
        try
        {
            if (asLoad) ErrorMessage = null;
            await work();
            return true;
        }
        catch (ApiException ex) when (ex.IsAuth)
        {
            return false;
        }
        catch (ApiException ex)
        {
            Report(ex.Message, asLoad);
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Report("Something unexpected went wrong. Please try again.", asLoad);
            return false;
        }
        finally
        {
            if (showBusy) IsBusy = false;
            IsRefreshing = false;
        }
    }

    private void Report(string message, bool asLoad)
    {
        if (asLoad) ErrorMessage = message;
        else ShowBanner(message, true);
    }

    protected static string Money(decimal v) => "R " + v.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-ZA"));
}
