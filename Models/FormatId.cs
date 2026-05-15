namespace ImageConverter.Models;

public readonly record struct FormatId(string Value)
{
    public override string ToString() => Value;
}

public static class KnownFormatIds
{
    public const string Bmp = "bmp";
    public const string Gif = "gif";
    public const string Jpeg = "jpeg";
    public const string Png = "png";
    public const string Tiff = "tiff";
    public const string Webp = "webp";
}
