using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Services;

public interface IEncoderConfig
{
    IImageEncoder CreateEncoder();
    long EstimateEncoderOverhead(long pixels);
}
