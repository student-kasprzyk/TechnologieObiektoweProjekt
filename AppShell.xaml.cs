using GraTerenowa.Services;
using GraTerenowa.Views;

namespace GeoMapsPrototype;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(LocationSetListPage), typeof(LocationSetListPage));
        Routing.RegisterRoute(nameof(TaskEditorPage), typeof(TaskEditorPage));
        Routing.RegisterRoute(nameof(TaskDetailPage), typeof(TaskDetailPage));
        Routing.RegisterRoute(nameof(QRScannerPage), typeof(QRScannerPage));
    }
}