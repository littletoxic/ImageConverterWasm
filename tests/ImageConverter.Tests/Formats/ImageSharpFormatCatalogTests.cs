using ImageConverter.Models.Formats;

namespace ImageConverter.Tests.Formats;

public sealed class ImageSharpFormatCatalogTests
{
    private readonly ImageSharpFormatCatalog _catalog = new();

    [Fact]
    public void EncodableFormats_ExposeAppLevelDescriptors()
    {
        var ids = _catalog.EncodableFormats.Select(f => f.Id.Value).ToHashSet();

        Assert.Contains(KnownFormatIds.Bmp, ids);
        Assert.Contains(KnownFormatIds.Gif, ids);
        Assert.Contains(KnownFormatIds.Jpeg, ids);
        Assert.Contains(KnownFormatIds.Png, ids);
        Assert.Contains(KnownFormatIds.Tiff, ids);
        Assert.Contains(KnownFormatIds.Webp, ids);
        Assert.All(_catalog.EncodableFormats, format =>
        {
            Assert.False(string.IsNullOrWhiteSpace(format.Id.Value));
            Assert.False(string.IsNullOrWhiteSpace(format.Name));
            Assert.NotEmpty(format.FileExtensions);
            Assert.All(format.FileExtensions, extension =>
                Assert.DoesNotContain('.', extension));
        });
    }

    [Fact]
    public void DefaultTargetFormat_IsPng()
    {
        Assert.Equal(new FormatId(KnownFormatIds.Png), _catalog.DefaultTargetFormat.Id);
    }

    [Fact]
    public void AcceptExtensions_AreProducedFromDescriptors()
    {
        var expected = string.Join(",",
            _catalog.EncodableFormats.SelectMany(f => f.FileExtensions).Select(e => $".{e}"));

        Assert.Equal(expected, _catalog.AcceptExtensions);
    }

    [Theory]
    [InlineData("photo.raw", KnownFormatIds.Png, "photo.png")]
    [InlineData("archive.preview.jpg", KnownFormatIds.Webp, "archive.preview.webp")]
    public void GetOutputFileName_UsesCatalogDefaultExtension(
        string originalName,
        string formatId,
        string expected)
    {
        var outputFileName = _catalog.GetOutputFileName(originalName, new FormatId(formatId));

        Assert.Equal(expected, outputFileName);
    }
}
