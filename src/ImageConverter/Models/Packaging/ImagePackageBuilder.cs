using System.IO.Compression;

namespace ImageConverter.Models.Packaging;

public interface IImagePackageBuilder
{
    Task<BuildImagePackageResult> BuildAsync(
        IReadOnlyList<ImagePackageEntrySource> entries,
        string packageFileName,
        CancellationToken cancellationToken = default);
}

public sealed record ImagePackageEntrySource(string EntryName, Stream Stream);
public sealed record ImagePackage(string FileName, MemoryStream Stream, int EntryCount);
public sealed record BuildImagePackageSucceeded(ImagePackage Package);
public sealed record BuildImagePackageEmpty;
public union BuildImagePackageResult(
    BuildImagePackageSucceeded,
    BuildImagePackageEmpty);

public sealed class ZipImagePackageBuilder : IImagePackageBuilder
{
    public async Task<BuildImagePackageResult> BuildAsync(
        IReadOnlyList<ImagePackageEntrySource> entries,
        string packageFileName,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
            return new BuildImagePackageEmpty();

        var packageStream = new MemoryStream();
        using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var source in entries)
            {
                var entry = archive.CreateEntry(source.EntryName, CompressionLevel.NoCompression);
                await using var entryStream = entry.Open();
                await CopyFromStartAsync(source.Stream, entryStream, cancellationToken);
            }
        }

        packageStream.Position = 0;
        return new BuildImagePackageSucceeded(new ImagePackage(packageFileName, packageStream, entries.Count));
    }

    private static async Task CopyFromStartAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var originalPosition = source.CanSeek ? source.Position : 0;
        try
        {
            if (source.CanSeek)
                source.Position = 0;

            await source.CopyToAsync(destination, cancellationToken);
        }
        finally
        {
            if (source.CanSeek)
                source.Position = originalPosition;
        }
    }
}
