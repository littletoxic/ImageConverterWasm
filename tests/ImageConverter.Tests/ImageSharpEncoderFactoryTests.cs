using ImageConverter.Models;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageConverter.Tests;

public sealed class ImageSharpEncoderFactoryTests
{
    [Fact]
    public void EncoderSettingsDefaults_MatchCurrentUiDefaults()
    {
        Assert.Equal(new FormatId(KnownFormatIds.Bmp), new BmpEncoderSettings().FormatId);
        Assert.False(new BmpEncoderSettings().SupportTransparency);

        Assert.Equal(new FormatId(KnownFormatIds.Gif), new GifEncoderSettings().FormatId);
        Assert.Null(new GifEncoderSettings().ColorTableMode);

        Assert.Equal(new FormatId(KnownFormatIds.Jpeg), new JpegEncoderSettings().FormatId);
        Assert.Equal(90, new JpegEncoderSettings().Quality);

        Assert.Equal(new FormatId(KnownFormatIds.Png), new PngEncoderSettings().FormatId);
        Assert.Equal(6, new PngEncoderSettings().CompressionLevel);
        Assert.Equal(TransparentColorMode.Preserve, new PngEncoderSettings().TransparentColorMode);

        Assert.Equal(new FormatId(KnownFormatIds.Tiff), new TiffEncoderSettings().FormatId);
        Assert.Null(new TiffEncoderSettings().Compression);

        var webp = new WebpEncoderSettings();
        Assert.Equal(new FormatId(KnownFormatIds.Webp), webp.FormatId);
        Assert.Equal(75, webp.Quality);
        Assert.False(webp.Lossless);
        Assert.Equal(4, webp.Method);
        Assert.Equal(60, webp.FilterStrength);
    }

    [Fact]
    public void Create_MapsJpegSettingsToImageSharpEncoder()
    {
        var factory = CreateFactory();

        var encoder = CreateInner<JpegEncoder>(factory, new JpegEncoderSettings(
            Quality: 82,
            ColorType: JpegColorType.Rgb,
            SkipMetadata: true));

        Assert.Equal(82, encoder.Quality);
        Assert.Equal(JpegColorType.Rgb, encoder.ColorType);
        Assert.True(encoder.SkipMetadata);
    }

    [Fact]
    public void Create_MapsPngSettingsToImageSharpEncoder()
    {
        var factory = CreateFactory();

        var encoder = CreateInner<PngEncoder>(factory, new PngEncoderSettings(
            CompressionLevel: 9,
            ColorType: PngColorType.RgbWithAlpha,
            Interlace: true,
            BitDepth: PngBitDepth.Bit8,
            FilterMethod: PngFilterMethod.Adaptive,
            TransparentColorMode: TransparentColorMode.Clear,
            SkipMetadata: true));

        Assert.Equal(PngCompressionLevel.Level9, encoder.CompressionLevel);
        Assert.Equal(PngColorType.RgbWithAlpha, encoder.ColorType);
        Assert.Equal(PngInterlaceMode.Adam7, encoder.InterlaceMethod);
        Assert.Equal(PngBitDepth.Bit8, encoder.BitDepth);
        Assert.Equal(PngFilterMethod.Adaptive, encoder.FilterMethod);
        Assert.Equal(TransparentColorMode.Clear, encoder.TransparentColorMode);
        Assert.True(encoder.SkipMetadata);
    }

    [Fact]
    public void Create_MapsRemainingFormatSettingsToImageSharpEncoders()
    {
        var factory = CreateFactory();

        var bmp = CreateInner<BmpEncoder>(factory, new BmpEncoderSettings(
            BmpBitsPerPixel.Bit32,
            SupportTransparency: true,
            SkipMetadata: true));
        Assert.Equal(BmpBitsPerPixel.Bit32, bmp.BitsPerPixel);
        Assert.True(bmp.SupportTransparency);
        Assert.True(bmp.SkipMetadata);

        var gif = CreateInner<GifEncoder>(factory, new GifEncoderSettings(
            FrameColorTableMode.Local,
            SkipMetadata: true));
        Assert.Equal(FrameColorTableMode.Local, gif.ColorTableMode);
        Assert.True(gif.SkipMetadata);

        var tiff = CreateInner<TiffEncoder>(factory, new TiffEncoderSettings(
            TiffCompression.Lzw,
            TiffBitsPerPixel.Bit24,
            TiffPhotometricInterpretation.Rgb,
            TiffPredictor.Horizontal,
            SkipMetadata: true));
        Assert.Equal(TiffCompression.Lzw, tiff.Compression);
        Assert.Equal(TiffBitsPerPixel.Bit24, tiff.BitsPerPixel);
        Assert.Equal(TiffPhotometricInterpretation.Rgb, tiff.PhotometricInterpretation);
        Assert.Equal(TiffPredictor.Horizontal, tiff.HorizontalPredictor);
        Assert.True(tiff.SkipMetadata);

        var webp = CreateInner<WebpEncoder>(factory, new WebpEncoderSettings(
            Quality: 64,
            Lossless: true,
            Method: 6,
            FilterStrength: 25,
            NearLossless: true,
            NearLosslessQuality: 80,
            TransparentColorMode: TransparentColorMode.Clear,
            SkipMetadata: true));
        Assert.Equal(WebpFileFormatType.Lossless, webp.FileFormat);
        Assert.Equal(64, webp.Quality);
        Assert.Equal(WebpEncodingMethod.BestQuality, webp.Method);
        Assert.Equal(25, webp.FilterStrength);
        Assert.True(webp.NearLossless);
        Assert.Equal(80, webp.NearLosslessQuality);
        Assert.Equal(TransparentColorMode.Clear, webp.TransparentColorMode);
        Assert.True(webp.SkipMetadata);
    }

    private static ImageSharpEncoderFactory CreateFactory() =>
        new(new ImageSharpFormatCatalog());

    private static TEncoder CreateInner<TEncoder>(
        ImageSharpEncoderFactory factory,
        EncoderSettings settings)
    {
        var adapter = Assert.IsType<ImageSharpFormatEncoder>(
            factory.Create(settings.FormatId, settings));
        return Assert.IsType<TEncoder>(adapter.Inner);
    }
}
