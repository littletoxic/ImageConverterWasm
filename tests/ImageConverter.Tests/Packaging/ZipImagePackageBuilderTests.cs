using System.IO.Compression;
using ImageConverter.Models;

namespace ImageConverter.Tests;

public sealed class ZipImagePackageBuilderTests
{
    [Fact]
    public async Task BuildAsync_CreatesZipWithEveryEntryNameAndContent()
    {
        var builder = new ZipImagePackageBuilder();
        var firstBytes = "first image"u8.ToArray();
        var secondBytes = "second image"u8.ToArray();

        var result = await builder.BuildAsync(
            [
                new ImagePackageEntrySource("first.webp", new MemoryStream(firstBytes)),
                new ImagePackageEntrySource("second.png", new MemoryStream(secondBytes))
            ],
            "converted-images.zip",
            TestContext.Current.CancellationToken);

        var package = Assert.IsType<BuildImagePackageSucceeded>(result.Value).Package;
        Assert.Equal("converted-images.zip", package.FileName);
        Assert.Equal(2, package.EntryCount);

        using var archive = new ZipArchive(package.Stream, ZipArchiveMode.Read, leaveOpen: true);
        AssertEntry(archive, "first.webp", firstBytes);
        AssertEntry(archive, "second.png", secondBytes);
    }

    [Fact]
    public async Task BuildAsync_ReturnsEmptyWithoutCreatingPackageWhenNoEntriesExist()
    {
        var builder = new ZipImagePackageBuilder();

        var result = await builder.BuildAsync(
            [],
            "converted-images.zip",
            TestContext.Current.CancellationToken);

        Assert.IsType<BuildImagePackageEmpty>(result.Value);
    }

    [Fact]
    public async Task BuildAsync_ReadsSourceStreamsFromStartAndRestoresOriginalPosition()
    {
        var builder = new ZipImagePackageBuilder();
        var bytes = "converted content"u8.ToArray();
        using var sourceStream = new MemoryStream(bytes);
        sourceStream.Position = 7;

        var result = await builder.BuildAsync(
            [new ImagePackageEntrySource("image.webp", sourceStream)],
            "converted-images.zip",
            TestContext.Current.CancellationToken);

        Assert.Equal(7, sourceStream.Position);
        var package = Assert.IsType<BuildImagePackageSucceeded>(result.Value).Package;
        using var archive = new ZipArchive(package.Stream, ZipArchiveMode.Read, leaveOpen: true);
        AssertEntry(archive, "image.webp", bytes);
    }

    private static void AssertEntry(ZipArchive archive, string entryName, byte[] expectedBytes)
    {
        var entry = Assert.Single(archive.Entries, e => e.FullName == entryName);
        using var entryStream = entry.Open();
        using var actual = new MemoryStream();
        entryStream.CopyTo(actual);
        Assert.Equal(expectedBytes, actual.ToArray());
    }
}
