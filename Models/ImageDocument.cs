using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageConverter.Models;

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

    public async Task LoadAsync(Stream stream)
    {
        _image?.Dispose();
        _image = await Image.LoadAsync(stream);
    }

    public async Task<ConversionResult> ConvertAsync(IImageEncoder? encoder)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        var formatEncoder = encoder is null
            ? formatCatalog.GetEncoder(TargetFormatId)
            : new ImageSharpEncoderAdapter(encoder);

        var outputStream = new MemoryStream();
        await formatEncoder.SaveAsync(_image, outputStream);

        outputStream.Position = 0;
        var outputFileName = formatCatalog.GetOutputFileName(FileName, TargetFormatId);
        return new ConversionResult(outputStream, outputFileName, outputStream.Length);
    }

    public string ToThumbnailDataUrl(int maxSize = 320)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        using var thumbnail = _image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));
        return thumbnail.ToBase64String(JpegFormat.Instance);
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}

public record ConversionResult(MemoryStream Stream, string OutputFileName, long Size);

internal sealed class ImageSharpEncoderAdapter(SixLabors.ImageSharp.Formats.IImageEncoder inner)
    : IImageFormatEncoder
{
    public Task SaveAsync(Image image, Stream stream) =>
        image.SaveAsync(stream, inner);
}
