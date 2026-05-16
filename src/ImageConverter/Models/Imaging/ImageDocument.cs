using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageConverter.Models.Imaging;

public sealed record EncodedImage(MemoryStream Stream, long Size);

public sealed class ImageDocument(string fileName) : IDisposable
{
    public const int ThumbnailMaxSize = 320;

    private Image? _image;

    public string FileName { get; } = fileName;
    public int Width => _image?.Width ?? 0;
    public int Height => _image?.Height ?? 0;
    public bool IsLoaded => _image is not null;

    public async Task LoadAsync(Stream stream)
    {
        _image?.Dispose();
        _image = await Image.LoadAsync(stream);
    }

    public async Task<EncodedImage> ConvertAsync(IImageEncoder encoder)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        var outputStream = new MemoryStream();
        await _image.SaveAsync(outputStream, encoder);
        outputStream.Position = 0;
        return new EncodedImage(outputStream, outputStream.Length);
    }

    public string CreatePreview(int maxSize)
    {
        if (_image is null)
            throw new InvalidOperationException("Image not loaded.");

        using var preview = _image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));

        return preview.ToBase64String(JpegFormat.Instance);
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}
