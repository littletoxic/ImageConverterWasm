using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageConverter.Models;

public sealed class ImageSharpFormatCatalog : IFormatCatalog
{
    private readonly IReadOnlyDictionary<FormatId, IImageFormat> _formatsById;

    public ImageSharpFormatCatalog()
    {
        var formats = Configuration.Default.ImageFormats
            .Where(CanEncode)
            .OrderBy(f => f.Name)
            .ToArray();

        EncodableFormats = [.. formats.Select(CreateDescriptor)];
        _formatsById = formats.ToDictionary(GetId, f => f);
        DefaultTargetFormat = EncodableFormats.FirstOrDefault(f => f.Id.Value == KnownFormatIds.Png)
            ?? EncodableFormats[0];
        AcceptExtensions = string.Join(",",
            EncodableFormats.SelectMany(f => f.FileExtensions).Select(e => $".{e}"));
    }

    public IReadOnlyList<ImageFormatDescriptor> EncodableFormats { get; }
    public ImageFormatDescriptor DefaultTargetFormat { get; }
    public string AcceptExtensions { get; }

    public string GetOutputFileName(string originalName, FormatId formatId) =>
        Path.GetFileNameWithoutExtension(originalName) + "." + GetDescriptor(formatId).DefaultExtension;

    public IImageFormatEncoder GetEncoder(FormatId formatId)
    {
        var format = GetFormat(formatId);
        var encoder = Configuration.Default.ImageFormatsManager.GetEncoder(format);
        return new ImageSharpFormatEncoder(encoder);
    }

    private static bool CanEncode(IImageFormat format)
    {
        try
        {
            Configuration.Default.ImageFormatsManager.GetEncoder(format);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ImageFormatDescriptor CreateDescriptor(IImageFormat format) =>
        new(GetId(format), format.Name, [.. format.FileExtensions]);

    private static FormatId GetId(IImageFormat format) =>
        new(format switch
        {
            PngFormat => KnownFormatIds.Png,
            _ => format.Name.ToLowerInvariant()
        });

    private ImageFormatDescriptor GetDescriptor(FormatId formatId) =>
        EncodableFormats.FirstOrDefault(f => f.Id == formatId)
        ?? throw new ArgumentException($"Unknown image format '{formatId}'.", nameof(formatId));

    private IImageFormat GetFormat(FormatId formatId) =>
        _formatsById.TryGetValue(formatId, out var format)
            ? format
            : throw new ArgumentException($"Unknown image format '{formatId}'.", nameof(formatId));

    private sealed class ImageSharpFormatEncoder(IImageEncoder inner) : IImageFormatEncoder
    {
        public Task SaveAsync(Image image, Stream stream) =>
            image.SaveAsync(stream, inner);
    }
}
