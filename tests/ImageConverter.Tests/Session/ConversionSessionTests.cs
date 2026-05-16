using ImageConverter.Models.Formats;
using ImageConverter.Models.Packaging;
using ImageConverter.Models.Session;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageConverter.Tests.Session;

public sealed class ConversionSessionTests
{
    [Fact]
    public async Task LoadAndConvertSingleImage_TransitionsToPendingThenDone()
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

        var convertResult = await session.ConvertImageAsync(itemId);

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
    public async Task CreatePreview_ReturnsDataUrlForLoadedImage()
    {
        var session = CreateSession();
        var sourceBytes = CreatePngBytes(width: 4, height: 5);
        var itemId = SingleAddedId(session.AddFiles(
            [new BrowserImageFile("source.png", sourceBytes.Length, _ => new MemoryStream(sourceBytes))]));

        await session.LoadImageAsync(itemId);
        var result = session.CreatePreview(itemId, 1024);

        var preview = Assert.IsType<CreatePreviewSucceeded>(result.Value);
        Assert.Equal(itemId, preview.ItemId);
        Assert.StartsWith("data:image/jpeg;base64,", preview.DataUrl);
    }

    [Fact]
    public void CreatePreview_ReturnsNotLoadedForUnloadedImage()
    {
        var session = CreateSession();
        var sourceBytes = CreatePngBytes(width: 4, height: 5);
        var itemId = SingleAddedId(session.AddFiles(
            [new BrowserImageFile("source.png", sourceBytes.Length, _ => new MemoryStream(sourceBytes))]));

        var result = session.CreatePreview(itemId, 1024);

        Assert.IsType<CreatePreviewNotLoaded>(result.Value);
    }

    [Fact]
    public void CreatePreview_ReturnsNotFoundForMissingImage()
    {
        var session = CreateSession();
        var itemId = Guid.NewGuid();

        var result = session.CreatePreview(itemId, 1024);

        Assert.Equal(itemId, Assert.IsType<CreatePreviewItemNotFound>(result.Value).ItemId);
    }

    [Fact]
    public async Task ConvertAll_ProcessesConvertibleItemsAndReportsProgress()
    {
        var session = CreateSession();
        var firstId = AddImage(session, "first.png", CreatePngBytes(width: 1, height: 1));
        var secondId = AddImage(session, "second.png", CreatePngBytes(width: 2, height: 2));
        await Task.WhenAll(session.LoadImageAsync(firstId), session.LoadImageAsync(secondId));

        var progress = new List<BatchConversionSnapshot>();

        var result = await session.ConvertAllAsync(() => progress.Add(session.Snapshot.Batch));

        var succeeded = Assert.IsType<ConvertAllSucceeded>(result.Value);
        Assert.Equal(2, succeeded.ConvertedCount);
        Assert.Contains(progress, p => p is { IsConverting: true, ConvertedCount: 0, TotalCount: 2 });
        Assert.Contains(progress, p => p is { IsConverting: true, ConvertedCount: 1, TotalCount: 2 });
        Assert.Contains(progress, p => p is { IsConverting: true, ConvertedCount: 2, TotalCount: 2 });
        Assert.Equal(new BatchConversionSnapshot(false, 2, 2), session.Snapshot.Batch);
        Assert.All(session.Snapshot.Items, item => Assert.Equal(ImageItemStatus.Done, item.Status));
        Assert.Equal(firstId, Assert.IsType<OpenConvertedImageSucceeded>(session.OpenConvertedImage(firstId).Value).ItemId);
        Assert.Equal(secondId, Assert.IsType<OpenConvertedImageSucceeded>(session.OpenConvertedImage(secondId).Value).ItemId);
    }

    [Fact]
    public async Task SetTargetFormat_ResetsCompletedResultsAndDisposesConvertedStream()
    {
        var session = CreateSession();
        var itemId = AddImage(session, "source.png", CreatePngBytes(width: 1, height: 1));
        await session.LoadImageAsync(itemId);
        await session.ConvertImageAsync(itemId);
        var stream = Assert.IsType<OpenConvertedImageSucceeded>(session.OpenConvertedImage(itemId).Value).Stream;

        session.SetTargetFormat(new FormatId(KnownFormatIds.Webp));

        var item = Assert.Single(session.Snapshot.Items);
        Assert.Equal(ImageItemStatus.Pending, item.Status);
        Assert.False(item.CanDownload);
        Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(1));
    }

    [Fact]
    public async Task RemoveItem_DisposesConvertedResult()
    {
        var session = CreateSession();
        var itemId = AddImage(session, "source.png", CreatePngBytes(width: 1, height: 1));
        await session.LoadImageAsync(itemId);
        await session.ConvertImageAsync(itemId);
        var stream = Assert.IsType<OpenConvertedImageSucceeded>(session.OpenConvertedImage(itemId).Value).Stream;

        var result = session.RemoveItem(itemId);

        Assert.IsType<RemoveItemSucceeded>(result.Value);
        Assert.Empty(session.Snapshot.Items);
        Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(1));
    }

    [Fact]
    public async Task Clear_DisposesConvertedResultsAndRemovesItems()
    {
        var session = CreateSession();
        var itemId = AddImage(session, "source.png", CreatePngBytes(width: 1, height: 1));
        await session.LoadImageAsync(itemId);
        await session.ConvertImageAsync(itemId);
        var stream = Assert.IsType<OpenConvertedImageSucceeded>(session.OpenConvertedImage(itemId).Value).Stream;

        session.Clear();

        Assert.Empty(session.Snapshot.Items);
        Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(1));
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
        new(new ImageSharpFormatCatalog(),
            new ZipImagePackageBuilder(),
            NullLogger<ConversionSession>.Instance);

    private static Guid AddImage(ConversionSession session, string fileName, byte[] bytes) =>
        SingleAddedId(session.AddFiles(
            [new BrowserImageFile(fileName, bytes.Length, _ => new MemoryStream(bytes))]));

    private static Guid SingleAddedId(IReadOnlyList<Guid> ids) => Assert.Single(ids);

    private static byte[] CreatePngBytes(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
