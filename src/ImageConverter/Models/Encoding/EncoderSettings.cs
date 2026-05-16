using ImageConverter.Models.Formats;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageConverter.Models.Encoding;

public abstract record EncoderSettings
{
    public bool SkipMetadata { get; init; }

    public abstract IImageEncoder BuildEncoder();

    public static EncoderSettings Default(FormatId formatId) => formatId.Value switch
    {
        KnownFormatIds.Bmp => new BmpEncoderSettings(),
        KnownFormatIds.Gif => new GifEncoderSettings(),
        KnownFormatIds.Jpeg => new JpegEncoderSettings(),
        KnownFormatIds.Png => new PngEncoderSettings(),
        KnownFormatIds.Tiff => new TiffEncoderSettings(),
        KnownFormatIds.Webp => new WebpEncoderSettings(),
        _ => throw new ArgumentException($"Unknown format '{formatId}'.", nameof(formatId))
    };
}

public sealed record BmpEncoderSettings(
    BmpBitsPerPixel? BitsPerPixel = null,
    bool SupportTransparency = false) : EncoderSettings
{
    public override IImageEncoder BuildEncoder() =>
        new BmpEncoder
        {
            BitsPerPixel = BitsPerPixel,
            SupportTransparency = SupportTransparency,
            SkipMetadata = SkipMetadata
        };
}

public sealed record GifEncoderSettings(
    FrameColorTableMode? ColorTableMode = null) : EncoderSettings
{
    public override IImageEncoder BuildEncoder() =>
        new GifEncoder
        {
            ColorTableMode = ColorTableMode,
            SkipMetadata = SkipMetadata
        };
}

public sealed record JpegEncoderSettings(
    int Quality = 90,
    JpegColorType? ColorType = null) : EncoderSettings
{
    public override IImageEncoder BuildEncoder() =>
        new JpegEncoder
        {
            Quality = Quality,
            ColorType = ColorType,
            SkipMetadata = SkipMetadata
        };
}

public sealed record PngEncoderSettings(
    int CompressionLevel = 6,
    PngColorType? ColorType = null,
    bool Interlace = false,
    PngBitDepth? BitDepth = null,
    PngFilterMethod? FilterMethod = null,
    TransparentColorMode TransparentColorMode = TransparentColorMode.Preserve) : EncoderSettings
{
    public override IImageEncoder BuildEncoder() =>
        new PngEncoder
        {
            CompressionLevel = (PngCompressionLevel)CompressionLevel,
            ColorType = ColorType,
            InterlaceMethod = Interlace ? PngInterlaceMode.Adam7 : PngInterlaceMode.None,
            BitDepth = BitDepth,
            FilterMethod = FilterMethod,
            TransparentColorMode = TransparentColorMode,
            SkipMetadata = SkipMetadata
        };
}

public sealed record TiffEncoderSettings(
    TiffCompression? Compression = null,
    TiffBitsPerPixel? BitsPerPixel = null,
    TiffPhotometricInterpretation? PhotometricInterpretation = null,
    TiffPredictor? Predictor = null) : EncoderSettings
{
    public override IImageEncoder BuildEncoder() =>
        new TiffEncoder
        {
            Compression = Compression,
            BitsPerPixel = BitsPerPixel,
            PhotometricInterpretation = PhotometricInterpretation,
            HorizontalPredictor = Predictor,
            SkipMetadata = SkipMetadata
        };
}

public sealed record WebpEncoderSettings(
    int Quality = 75,
    bool Lossless = false,
    int Method = 4,
    int FilterStrength = 60,
    bool NearLossless = false,
    int NearLosslessQuality = 60,
    TransparentColorMode TransparentColorMode = TransparentColorMode.Preserve) : EncoderSettings
{
    public override IImageEncoder BuildEncoder() =>
        new WebpEncoder
        {
            FileFormat = Lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
            Quality = Quality,
            Method = (WebpEncodingMethod)Method,
            FilterStrength = FilterStrength,
            NearLossless = NearLossless,
            NearLosslessQuality = NearLosslessQuality,
            TransparentColorMode = TransparentColorMode,
            SkipMetadata = SkipMetadata
        };
}
