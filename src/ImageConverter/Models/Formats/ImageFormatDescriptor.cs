using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Models.Formats;

public sealed record ImageFormatDescriptor(
    FormatId Id,
    string Name,
    IReadOnlyList<string> FileExtensions)
{
    public string DefaultExtension => FileExtensions[0];
}

public interface IFormatCatalog
{
    IReadOnlyList<ImageFormatDescriptor> EncodableFormats { get; }
    ImageFormatDescriptor DefaultTargetFormat { get; }
    string AcceptExtensions { get; }
    string GetOutputFileName(string originalName, FormatId formatId);
    IImageEncoder GetEncoder(FormatId formatId);
}
