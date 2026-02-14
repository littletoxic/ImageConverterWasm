using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageConverter.Services;

public record WebpEncoderConfig(int Quality = 75, bool Lossless = false) : IEncoderConfig
{
    public IImageEncoder CreateEncoder() =>
        new WebpEncoder
        {
            FileFormat = Lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
            Quality = Quality
        };

    public long EstimateEncoderOverhead(long pixels) =>
        Lossless ? pixels * 12 : pixels * 4;
}
