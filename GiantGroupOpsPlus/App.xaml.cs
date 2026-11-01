using GiantGroupOpsPlus.Services;

namespace GiantGroupOpsPlus;

public partial class App : Application
{
    private readonly IPhotoQueueService _queue;

    public App(IPhotoQueueService queue)
    {
        InitializeComponent();
        _queue = queue;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // No sign-in: start the photo queue straight away so photos saved on an earlier run upload now.
        _ = _queue.InitializeAsync();
        var window = new Window(new AppShell());
        // Coming back to the app (e.g. after the camera or a dead zone) – flush any waiting photos.
        window.Resumed += (_, _) => _ = _queue.ProcessAsync();
        return window;
    }
}
