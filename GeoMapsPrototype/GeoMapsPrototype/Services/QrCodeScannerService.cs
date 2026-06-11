using GraTerenowa.Views;
namespace GraTerenowa.Services;

public class QRScannerService
{
    // Wynik ustawiany przez QRScannerPage po udanym skanie
    public static string? LastScannedCode { get; set; }

    public async Task<string?> ScanAsync()
    {
        LastScannedCode = null;

        await Shell.Current.GoToAsync(nameof(QRScannerPage));

        // QRScannerPage po skanie ustawia LastScannedCode i wraca
        return LastScannedCode;
    }
}