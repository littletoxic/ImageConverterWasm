using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageConverter.Models;

public interface IImageFormatEncoderFactory
{
    IImageFormatEncoder Create(FormatId formatId, EncoderSettings? settings);
}

public sealed class ImageSharpEncoderFactory(IFormatCatalog formatCatalog) : IImageFormatEncoderFactory
{
    public IImageFormatEncoder Create(FormatId formatId, EncoderSettings? settings)
    {
        if (settings is null || settings.FormatId != formatId)
            return formatCatalog.GetEncoder(formatId);

        return settings switch
        {
            BmpEncoderSettings bmp => new ImageSharpFormatEncoder(new BmpEncoder
            {
                BitsPerPixel = bmp.BitsPerPixel,
                SupportTransparency = bmp.SupportTransparency,
                SkipMetadata = bmp.SkipMetadata
            }),
            GifEncoderSettings gif => new ImageSharpFormatEncoder(new GifEncoder
            {
                ColorTableMode = gif.ColorTableMode,
                SkipMetadata = gif.SkipMetadata
            }),
            JpegEncoderSettings jpeg => new ImageSharpFormatEncoder(new JpegEncoder
            {
                Quality = jpeg.Quality,
                ColorType = jpeg.ColorType,
                SkipMetadata = jpeg.SkipMetadata
            }),
            PngEncoderSettings png => new ImageSharpFormatEncoder(new PngEncoder
            {
                CompressionLevel = (PngCompressionLevel)png.CompressionLevel,
                ColorType = png.ColorType,
                InterlaceMethod = png.Interlace ? PngInterlaceMode.Adam7 : PngInterlaceMode.None,
                BitDepth = png.BitDepth,
                FilterMethod = png.FilterMethod,
                TransparentColorMode = png.TransparentColorMode,
                SkipMetadata = png.SkipMetadata
            }),
            TiffEncoderSettings tiff => new ImageSharpFormatEncoder(new TiffEncoder
            {
                Compression = tiff.Compression,
                BitsPerPixel = tiff.BitsPerPixel,
                PhotometricInterpretation = tiff.PhotometricInterpretation,
                HorizontalPredictor = tiff.Predictor,
                SkipMetadata = tiff.SkipMetadata
            }),
            WebpEncoderSettings webp => new ImageSharpFormatEncoder(new WebpEncoder
            {
                FileFormat = webp.Lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
                Quality = webp.Quality,
                Method = (WebpEncodingMethod)webp.Method,
                FilterStrength = webp.FilterStrength,
                NearLossless = webp.NearLossless,
                NearLosslessQuality = webp.NearLosslessQuality,
                TransparentColorMode = webp.TransparentColorMode,
                SkipMetadata = webp.SkipMetadata
            }),
            _ => formatCatalog.GetEncoder(formatId)
        };
    }
}

public sealed class ImageSharpFormatEncoder(IImageEncoder inner) : IImageFormatEncoder
{
    public IImageEncoder Inner { get; } = inner;

    public Task SaveAsync(Image image, Stream stream) =>
        image.SaveAsync(stream, Inner);
}
