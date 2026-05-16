using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageConverter.Models;

public abstract record EncoderSettings(FormatId FormatId, bool SkipMetadata);

public sealed record BmpEncoderSettings(
    BmpBitsPerPixel? BitsPerPixel = null,
    bool SupportTransparency = false,
    bool SkipMetadata = false)
    : EncoderSettings(new(KnownFormatIds.Bmp), SkipMetadata);

public sealed record GifEncoderSettings(
    FrameColorTableMode? ColorTableMode = null,
    bool SkipMetadata = false)
    : EncoderSettings(new(KnownFormatIds.Gif), SkipMetadata);

public sealed record JpegEncoderSettings(
    int Quality = 90,
    JpegColorType? ColorType = null,
    bool SkipMetadata = false)
    : EncoderSettings(new(KnownFormatIds.Jpeg), SkipMetadata);

public sealed record PngEncoderSettings(
    int CompressionLevel = 6,
    PngColorType? ColorType = null,
    bool Interlace = false,
    PngBitDepth? BitDepth = null,
    PngFilterMethod? FilterMethod = null,
    TransparentColorMode TransparentColorMode = TransparentColorMode.Preserve,
    bool SkipMetadata = false)
    : EncoderSettings(new(KnownFormatIds.Png), SkipMetadata);

public sealed record TiffEncoderSettings(
    TiffCompression? Compression = null,
    TiffBitsPerPixel? BitsPerPixel = null,
    TiffPhotometricInterpretation? PhotometricInterpretation = null,
    TiffPredictor? Predictor = null,
    bool SkipMetadata = false)
    : EncoderSettings(new(KnownFormatIds.Tiff), SkipMetadata);

public sealed record WebpEncoderSettings(
    int Quality = 75,
    bool Lossless = false,
    int Method = 4,
    int FilterStrength = 60,
    bool NearLossless = false,
    int NearLosslessQuality = 60,
    TransparentColorMode TransparentColorMode = TransparentColorMode.Preserve,
    bool SkipMetadata = false)
    : EncoderSettings(new(KnownFormatIds.Webp), SkipMetadata);
