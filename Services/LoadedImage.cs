using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageConverter.Services;

public sealed class LoadedImage(string fileName, long fileSize, IImageFormat targetFormat) : IDisposable
{
    private Image? _image;

    public string FileName { get; } = fileName;
    public long FileSize { get; } = fileSize;
    public int Width => _image?.Width ?? 0;
    public int Height => _image?.Height ?? 0;
    public string SourceFormat => _image?.Metadata.DecodedImageFormat?.Name ?? "";
    public bool IsLoaded => _image is not null;
    public IImageFormat TargetFormat { get; set; } = targetFormat;

    public async Task LoadAsync(Stream stream)
    {
        _image?.Dispose();
        _image = await Image.LoadAsync(stream);
    }

    public async Task<ConversionResult> ConvertAsync(IEncoderConfig? encoderConfig)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        var encoder = encoderConfig?.CreateEncoder()
            ?? Configuration.Default.ImageFormatsManager.GetEncoder(TargetFormat);

        var outputStream = new MemoryStream();
        await _image.SaveAsync(outputStream, encoder);

        outputStream.Position = 0;
        var outputFileName = ImageFormatInfo.GetOutputFileName(FileName, TargetFormat);
        return new ConversionResult(outputStream, outputFileName, outputStream.Length);
    }

    public long EstimateMemory(IEncoderConfig? encoderConfig)
    {
        long pixels = (long)Width * Height;
        long imageMemory = pixels * 4;
        long encoderOverhead = encoderConfig?.EstimateEncoderOverhead(pixels) ?? pixels * 2;
        return imageMemory + encoderOverhead;
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
