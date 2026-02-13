using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Services;

public static class ImageFormatInfo
{
    public static IReadOnlyList<IImageFormat> EncodableFormats { get; } = Configuration.Default.ImageFormats
        .Where(f =>
        {
            try { Configuration.Default.ImageFormatsManager.GetEncoder(f); return true; }
            catch { return false; }
        })
        .OrderBy(f => f.Name)
        .ToArray();

    public static string AcceptExtensions { get; } = string.Join(",",
        EncodableFormats.SelectMany(f => f.FileExtensions).Select(e => $".{e}"));

    public static string GetOutputFileName(string originalName, IImageFormat format) =>
        Path.GetFileNameWithoutExtension(originalName) + "." + format.FileExtensions.First();

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public static string FormatPixelCount(long pixels) =>
        $"{pixels / 1_000_000.0:F1}M";
}
