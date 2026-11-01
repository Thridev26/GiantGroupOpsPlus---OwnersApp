namespace GiantGroupOpsPlus.Services;

public interface ISettingsService
{
    string ServerUrl { get; set; }
    bool DemoMode { get; set; }
    bool WifiOnlyUploads { get; set; }
    string LastUsername { get; set; }
    void ResetServerUrl();
}

public class SettingsService : ISettingsService
{
    public const string DefaultServerUrl = "https://ist-wst-group-1.azurewebsites.net/MobileApi.ashx";

    public string ServerUrl
    {
        get => Preferences.Get(nameof(ServerUrl), DefaultServerUrl);
        set => Preferences.Set(nameof(ServerUrl), (value ?? "").Trim().TrimEnd('/'));
    }

    public bool DemoMode
    {
        get => Preferences.Get(nameof(DemoMode), false);
        set => Preferences.Set(nameof(DemoMode), value);
    }

    public bool WifiOnlyUploads
    {
        get => Preferences.Get(nameof(WifiOnlyUploads), false);
        set => Preferences.Set(nameof(WifiOnlyUploads), value);
    }

    public string LastUsername
    {
        get => Preferences.Get(nameof(LastUsername), "");
        set => Preferences.Set(nameof(LastUsername), value ?? "");
    }

    public void ResetServerUrl() => Preferences.Remove(nameof(ServerUrl));
}
