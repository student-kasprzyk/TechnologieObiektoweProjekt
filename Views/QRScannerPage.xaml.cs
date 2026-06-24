using GraTerenowa.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace GraTerenowa.Views;

public partial class QRScannerPage : ContentPage
{
    private bool _isProcessing = false;
    private CameraBarcodeReaderView _cameraView; // Definiujemy pole kamery ręcznie

    public QRScannerPage()
    {
        InitializeComponent();

        // Tworzymy kontrolkę aparatu bezpośrednio w C#
        _cameraView = new CameraBarcodeReaderView
        {
            IsDetecting = true
        };

        // Podpinamy Twój event wykrywania kodu
        _cameraView.BarcodesDetected += OnBarcodesDetected;

        // Dodajemy aparat do przygotowanego w XAML grida
        ScannerContainer.Children.Add(_cameraView);
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;
        _isProcessing = true;

        var result = e.Results.FirstOrDefault();
        if (result is null)
        {
            _isProcessing = false;
            return;
        }

        QRScannerService.LastScannedCode = result.Value;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.GoToAsync("..");
        });
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        QRScannerService.LastScannedCode = null;
        await Shell.Current.GoToAsync("..");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isProcessing = false;

        if (_cameraView != null)
        {
            _cameraView.IsDetecting = true; // W nowej bibliotece sterujemy flagą IsDetecting
            _cameraView.IsEnabled = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_cameraView != null)
        {
            _cameraView.IsDetecting = false;
            _cameraView.IsEnabled = false;
        }
    }
}