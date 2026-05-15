using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Models;

public sealed class ImageSharpCustomEncoder(IImageEncoder inner) : IImageFormatEncoder
{
    public Task SaveAsync(Image image, Stream stream) =>
        image.SaveAsync(stream, inner);
}
