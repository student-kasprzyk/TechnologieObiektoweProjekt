using QRCoder;

namespace GraTerenowa.Services;

public class QRCodeGeneratorService
{
    // Generuje obrazek QR jako tablicę bajtów PNG
    public byte[] GeneratePng(string content, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
                                   content,
                                   QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(data);

        return qrCode.GetGraphic(pixelsPerModule);
    }

    // Generuje i zapisuje PNG na dysk (do wydruku)
    public async Task SavePngAsync(string content,
                                   string filePath,
                                   int pixelsPerModule = 10)
    {
        var bytes = GeneratePng(content, pixelsPerModule);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    // Generuje ImageSource gotowy do wyświetlenia w MAUI Image
    public ImageSource GenerateImageSource(string content,
                                           int pixelsPerModule = 10)
    {
        var bytes = GeneratePng(content, pixelsPerModule);
        var stream = new MemoryStream(bytes);
        return ImageSource.FromStream(() => stream);
    }
}