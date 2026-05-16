namespace ImageConverter.Models;

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
    IImageFormatEncoder GetEncoder(FormatId formatId);
}

public interface IImageFormatEncoder
{
    Task SaveAsync(SixLabors.ImageSharp.Image image, Stream stream);
}
