using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace ImageConverter.Services;

public record JpegEncoderConfig(int Quality = 90) : IEncoderConfig
{
    public IImageEncoder CreateEncoder() =>
        new JpegEncoder { Quality = Quality };

    public long EstimateEncoderOverhead(long pixels) =>
        1 * 1024 * 1024;
}
