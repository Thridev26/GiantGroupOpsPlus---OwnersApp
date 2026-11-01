using GiantGroupOpsPlus.Services;
using GiantGroupOpsPlus.ViewModels;
using GiantGroupOpsPlus.Views;
using Microsoft.Extensions.Logging;

namespace GiantGroupOpsPlus;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---- services
        var s = builder.Services;
        s.AddSingleton<ISettingsService, SettingsService>();
        s.AddSingleton<IDialogService, DialogService>();
        s.AddSingleton<AppState>();

        // One long-lived HttpClient (avoids socket exhaustion). Timeouts are applied per request in ApiClient.
        s.AddSingleton(_ => new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
        s.AddSingleton<ApiClient>();
        s.AddSingleton<DemoApiClient>();
        s.AddSingleton<IApiClient, ApiClientRouter>();

        s.AddSingleton<IPhotoQueueService, PhotoQueueService>();
        s.AddSingleton<IPhotoCache, PhotoCache>();
        s.AddSingleton<IPhotoCaptureService, PhotoCaptureService>();

        // ---- view models + pages (transient: a fresh instance per navigation)
        s.AddTransient<DashboardViewModel>();
        s.AddTransient<RequestsViewModel>();
        s.AddTransient<RequestDetailViewModel>();
        s.AddTransient<JobsViewModel>();
        s.AddTransient<JobDetailViewModel>();
        s.AddTransient<PhotosViewModel>();
        s.AddTransient<PhotoViewerViewModel>();
        s.AddTransient<UploadQueueViewModel>();
        s.AddTransient<SettingsViewModel>();
        s.AddTransient<CalendarViewModel>();
        s.AddTransient<QuotesViewModel>();
        s.AddTransient<QuoteDetailViewModel>();
        s.AddTransient<QuoteCreateViewModel>();
        s.AddTransient<ClientsViewModel>();
        s.AddTransient<ClientDetailViewModel>();
        s.AddTransient<ClientEditViewModel>();
        s.AddTransient<NewRequestViewModel>();
        s.AddTransient<AllocationsViewModel>();
        s.AddTransient<AssignViewModel>();
        s.AddTransient<PaymentsViewModel>();
        s.AddTransient<PaymentDetailViewModel>();
        s.AddTransient<ReportsViewModel>();
        s.AddTransient<HelpViewModel>();

        s.AddTransient<DashboardPage>();
        s.AddTransient<RequestsPage>();
        s.AddTransient<RequestDetailPage>();
        s.AddTransient<JobsPage>();
        s.AddTransient<JobDetailPage>();
        s.AddTransient<PhotosPage>();
        s.AddTransient<PhotoViewerPage>();
        s.AddTransient<UploadQueuePage>();
        s.AddTransient<SettingsPage>();
        s.AddTransient<CalendarPage>();
        s.AddTransient<QuotesPage>();
        s.AddTransient<QuoteDetailPage>();
        s.AddTransient<QuoteCreatePage>();
        s.AddTransient<ClientsPage>();
        s.AddTransient<ClientDetailPage>();
        s.AddTransient<ClientEditPage>();
        s.AddTransient<NewRequestPage>();
        s.AddTransient<AllocationsPage>();
        s.AddTransient<AssignPage>();
        s.AddTransient<PaymentsPage>();
        s.AddTransient<PaymentDetailPage>();
        s.AddTransient<ReportsPage>();
        s.AddTransient<HelpPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
