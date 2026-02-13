using ImageConverter.Components;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Services;

public sealed class ImageConversionJob : IDisposable
{
    private Image? _image;

    public string FileName { get; }
    public long FileSize { get; }
    public int Width => _image?.Width ?? 0;
    public int Height => _image?.Height ?? 0;
    public string SourceFormat => _image?.Metadata.DecodedImageFormat?.Name ?? "";
    public bool IsLoaded => _image is not null;
    public IImageFormat TargetFormat { get; set; }

    public ImageConversionJob(string fileName, long fileSize, IImageFormat targetFormat)
    {
        FileName = fileName;
        FileSize = fileSize;
        TargetFormat = targetFormat;
    }

    public async Task LoadAsync(Stream stream)
    {
        _image?.Dispose();
        _image = await Image.LoadAsync(stream);
    }

    public async Task<ConversionResult> ConvertAsync(EncoderOptionsBase? encoderOptions)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        var encoder = encoderOptions?.CreateEncoder()
            ?? Configuration.Default.ImageFormatsManager.GetEncoder(TargetFormat);

        var outputStream = new MemoryStream();
        await _image.SaveAsync(outputStream, encoder);

        outputStream.Position = 0;
        var outputFileName = ImageFormatInfo.GetOutputFileName(FileName, TargetFormat);
        return new ConversionResult(outputStream, outputFileName, outputStream.Length);
    }

    public long EstimateMemory(EncoderOptionsBase? encoderOptions)
    {
        long pixels = (long)Width * Height;
        long imageMemory = pixels * 4;
        long encoderOverhead = encoderOptions?.EstimateEncoderOverhead(pixels) ?? pixels * 2;
        return imageMemory + encoderOverhead;
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}

public record ConversionResult(MemoryStream Stream, string OutputFileName, long Size);
