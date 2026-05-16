using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ImageConverter.Models.Imaging;

public sealed record ImagePreviewRequest(int MaxSize)
{
    public static ImagePreviewRequest Thumbnail { get; } = new(320);
}

public interface IImagePreviewBuilder
{
    string CreatePreview(ImageDocument document, ImagePreviewRequest request);
}

public sealed class ImageSharpImagePreviewBuilder : IImagePreviewBuilder
{
    public string CreatePreview(ImageDocument document, ImagePreviewRequest request)
    {
        using var preview = document.LoadedImage.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(request.MaxSize, request.MaxSize),
            Mode = ResizeMode.Max
        }));

        return preview.ToBase64String(JpegFormat.Instance);
    }
}
