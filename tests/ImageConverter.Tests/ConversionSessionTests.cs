using ImageConverter.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageConverter.Tests;

public sealed class ConversionSessionTests
{
    [Fact]
    public async Task LoadAndConvertSingleImage_UpdatesSnapshotStates()
    {
        var session = CreateSession();
        var sourceBytes = CreatePngBytes(width: 2, height: 3);

        var itemId = SingleAddedId(session.AddFiles(
            [new BrowserImageFile("source.png", sourceBytes.Length, _ => new MemoryStream(sourceBytes))]));

        var loadingItem = Assert.Single(session.Snapshot.Items);
        Assert.Equal(ImageItemStatus.Loading, loadingItem.Status);
        Assert.False(loadingItem.CanConvert);

        var loadResult = await session.LoadImageAsync(itemId);

        Assert.IsType<LoadImageSucceeded>(loadResult.Value);
        var loadedItem = Assert.Single(session.Snapshot.Items);
        Assert.Equal(ImageItemStatus.Pending, loadedItem.Status);
        Assert.Equal(2, loadedItem.Width);
        Assert.Equal(3, loadedItem.Height);
        Assert.NotNull(loadedItem.ThumbnailUrl);
        Assert.True(loadedItem.CanConvert);

        var conversionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.UpdateEncoder(new DelayedPngEncoder(conversionGate.Task));
        var convertTask = session.ConvertImageAsync(itemId);
        Assert.Equal(ImageItemStatus.Converting, Assert.Single(session.Snapshot.Items).Status);
        conversionGate.SetResult();
        var convertResult = await convertTask;

        Assert.IsType<ConvertImageSucceeded>(convertResult.Value);
        var convertedItem = Assert.Single(session.Snapshot.Items);
        Assert.Equal(ImageItemStatus.Done, convertedItem.Status);
        Assert.True(convertedItem.CanDownload);
        Assert.True(convertedItem.ResultSize > 0);
    }

    [Fact]
    public async Task OpenConvertedImage_ReturnsConvertedStreamAndOutputName()
    {
        var session = CreateSession();
        var sourceBytes = CreatePngBytes(width: 1, height: 1);
        var itemId = SingleAddedId(session.AddFiles(
            [new BrowserImageFile("source.raw", sourceBytes.Length, _ => new MemoryStream(sourceBytes))]));

        await session.LoadImageAsync(itemId);
        await session.ConvertImageAsync(itemId);

        var openResult = session.OpenConvertedImage(itemId);

        var download = Assert.IsType<OpenConvertedImageSucceeded>(openResult.Value);
        Assert.Equal("source.png", download.OutputFileName);
        Assert.True(download.Stream.Length > 0);
        Assert.Equal(0, download.Stream.Position);
    }

    [Fact]
    public async Task LoadImageFailure_UpdatesErrorSnapshot()
    {
        var session = CreateSession();
        var invalidBytes = "not an image"u8.ToArray();
        var itemId = SingleAddedId(session.AddFiles(
            [new BrowserImageFile("broken.png", invalidBytes.Length, _ => new MemoryStream(invalidBytes))]));

        var result = await session.LoadImageAsync(itemId);

        Assert.IsType<LoadImageFailed>(result.Value);
        var item = Assert.Single(session.Snapshot.Items);
        Assert.Equal(ImageItemStatus.Error, item.Status);
        Assert.StartsWith("加载失败：", item.ErrorMessage);
    }

    private static ConversionSession CreateSession() =>
        new(new ImageSharpFormatCatalog(), NullLogger<ConversionSession>.Instance);

    private static Guid SingleAddedId(AddFilesResult result) =>
        Assert.Single(Assert.IsType<AddFilesSucceeded>(result.Value).ItemIds);

    private static byte[] CreatePngBytes(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private sealed class DelayedPngEncoder(Task gate) : IImageFormatEncoder
    {
        public async Task SaveAsync(Image image, Stream stream)
        {
            await gate;
            await image.SaveAsPngAsync(stream);
        }
    }
}
