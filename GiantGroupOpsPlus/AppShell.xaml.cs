using GiantGroupOpsPlus.Views;

namespace GiantGroupOpsPlus;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed (non-tab) pages
        Routing.RegisterRoute("requestdetail", typeof(RequestDetailPage));
        Routing.RegisterRoute("jobdetail", typeof(JobDetailPage));
        Routing.RegisterRoute("photos", typeof(PhotosPage));
        Routing.RegisterRoute("photoviewer", typeof(PhotoViewerPage));
        Routing.RegisterRoute("queue", typeof(UploadQueuePage));

        // Full-app pages
        Routing.RegisterRoute("quotes", typeof(QuotesPage));
        Routing.RegisterRoute("quotedetail", typeof(QuoteDetailPage));
        Routing.RegisterRoute("quotecreate", typeof(QuoteCreatePage));
        Routing.RegisterRoute("clients", typeof(ClientsPage));
        Routing.RegisterRoute("clientdetail", typeof(ClientDetailPage));
        Routing.RegisterRoute("clientedit", typeof(ClientEditPage));
        Routing.RegisterRoute("newrequest", typeof(NewRequestPage));
        Routing.RegisterRoute("allocations", typeof(AllocationsPage));
        Routing.RegisterRoute("assign", typeof(AssignPage));
        Routing.RegisterRoute("payments", typeof(PaymentsPage));
        Routing.RegisterRoute("paymentdetail", typeof(PaymentDetailPage));
        Routing.RegisterRoute("reports", typeof(ReportsPage));
        Routing.RegisterRoute("help", typeof(HelpPage));
    }
}
