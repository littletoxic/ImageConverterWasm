using ImageConverter.Models.Formats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Models.Imaging;

public sealed class ImageDocument(
    string fileName,
    FormatId targetFormatId,
    IFormatCatalog formatCatalog) : IDisposable
{
    private Image? _image;

    public string FileName { get; } = fileName;
    public int Width => _image?.Width ?? 0;
    public int Height => _image?.Height ?? 0;
    public bool IsLoaded => _image is not null;
    public FormatId TargetFormatId { get; set; } = targetFormatId;
    internal Image LoadedImage => _image ?? throw new InvalidOperationException("Image not loaded.");

    public async Task LoadAsync(Stream stream)
    {
        _image?.Dispose();
        _image = await Image.LoadAsync(stream);
    }

    public async Task<ConversionResult> ConvertAsync(IImageEncoder encoder)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        var outputStream = new MemoryStream();
        await _image.SaveAsync(outputStream, encoder);

        outputStream.Position = 0;
        var outputFileName = formatCatalog.GetOutputFileName(FileName, TargetFormatId);
        return new ConversionResult(outputStream, outputFileName, outputStream.Length);
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}

public record ConversionResult(MemoryStream Stream, string OutputFileName, long Size);
