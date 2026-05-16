using ImageConverter.Models.Encoding;
using ImageConverter.Models.Formats;
using ImageConverter.Models.Imaging;
using ImageConverter.Models.Packaging;

namespace ImageConverter.Models.Session;

public interface IConversionSession
{
    ConversionSessionSnapshot Snapshot { get; }
    AddFilesResult AddFiles(IReadOnlyList<BrowserImageFile> files);
    Task<LoadImageResult> LoadImageAsync(Guid itemId);
    void SetTargetFormat(FormatId formatId);
    void UpdateEncoderSettings(EncoderSettings settings);
    void SetSkipMetadata(bool value);
    Task<ConvertImageResult> ConvertImageAsync(Guid itemId);
    Task<ConvertAllResult> ConvertAllAsync(Action? progressChanged = null);
    CreatePreviewResult CreatePreview(Guid itemId, int maxSize);
    OpenConvertedImageResult OpenConvertedImage(Guid itemId);
    Task<BuildConvertedPackageResult> BuildConvertedPackageAsync();
    RemoveItemResult RemoveItem(Guid itemId);
    void Clear();
}

public sealed class ConversionSession(
    IFormatCatalog formatCatalog,
    IImagePreviewBuilder previewBuilder,
    IImagePackageBuilder packageBuilder,
    ILogger<ConversionSession> logger) : IConversionSession
{
    private const long MaxFileSize = 50 * 1024 * 1024;

    private readonly List<SessionImageItem> _items = [];
    private FormatId _targetFormatId = formatCatalog.DefaultTargetFormat.Id;
    private EncoderSettings _encoderSettings = EncoderSettings.Default(formatCatalog.DefaultTargetFormat.Id);
    private bool _skipMetadata;
    private BatchConversionSnapshot _batch = new(false, 0, 0);

    public ConversionSessionSnapshot Snapshot => new(
        [.. _items.Select(CreateSnapshot)],
        _targetFormatId,
        _batch,
        _items.Any(CanConvert),
        _items.Any(i => i.Status is ImageItemStatus.Done));

    public AddFilesResult AddFiles(IReadOnlyList<BrowserImageFile> files)
    {
        var ids = new List<Guid>(files.Count);
        foreach (var file in files)
        {
            var item = new SessionImageItem(file);
            _items.Add(item);
            ids.Add(item.Id);
        }

        return new AddFilesSucceeded(ids);
    }

    public async Task<LoadImageResult> LoadImageAsync(Guid itemId)
    {
        var item = Find(itemId);
        if (item is null)
            return new LoadImageItemNotFound(itemId);

        try
        {
            item.Job = new ImageDocument(item.File.FileName, _targetFormatId, formatCatalog);
            await using var stream = item.File.OpenReadStream(MaxFileSize);
            await item.Job.LoadAsync(stream);
            item.ThumbnailUrl = previewBuilder.CreatePreview(item.Job, ImagePreviewRequest.Thumbnail);
            item.Status = ImageItemStatus.Pending;
            item.ErrorMessage = null;
            return new LoadImageSucceeded(itemId);
        }
        catch (Exception ex)
        {
            item.Status = ImageItemStatus.Error;
            item.ErrorMessage = $"加载失败：{ex.Message}";
            logger.LogError(ex, "Failed to load {FileName}", item.File.FileName);
            return new LoadImageFailed(itemId, item.ErrorMessage);
        }
    }

    public void SetTargetFormat(FormatId formatId)
    {
        _targetFormatId = formatId;
        _encoderSettings = EncoderSettings.Default(formatId) with { SkipMetadata = _skipMetadata };

        foreach (var item in _items)
        {
            if (item.Job is not null)
                item.Job.TargetFormatId = formatId;

            if (item.Status == ImageItemStatus.Done)
            {
                item.Result?.Stream.Dispose();
                item.Result = null;
                item.Status = ImageItemStatus.Pending;
            }
        }
    }

    public void UpdateEncoderSettings(EncoderSettings settings) =>
        _encoderSettings = settings with { SkipMetadata = _skipMetadata };

    public void SetSkipMetadata(bool value)
    {
        _skipMetadata = value;
        _encoderSettings = _encoderSettings with { SkipMetadata = value };
    }

    public async Task<ConvertImageResult> ConvertImageAsync(Guid itemId)
    {
        var item = Find(itemId);
        if (item is null)
            return new ConvertImageItemNotFound(itemId);
        if (item.Job is not { IsLoaded: true })
            return new ConvertImageNotLoaded(itemId);

        item.Status = ImageItemStatus.Converting;
        item.ErrorMessage = null;
        item.Result?.Stream.Dispose();
        item.Result = null;

        try
        {
            item.Job.TargetFormatId = _targetFormatId;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var encoder = _encoderSettings.BuildEncoder();
            var result = await item.Job.ConvertAsync(encoder);
            sw.Stop();

            logger.LogInformation("Converted {FileName} to {Format} in {Elapsed}ms ({Size})",
                item.File.FileName, _targetFormatId, sw.ElapsedMilliseconds,
                FormatInfo.FormatFileSize(result.Size));
            item.Result = result;
            item.Status = ImageItemStatus.Done;
            return new ConvertImageSucceeded(itemId);
        }
        catch (OutOfMemoryException)
        {
            item.Status = ImageItemStatus.Error;
            item.ErrorMessage = "内存不足，请尝试较小的图像。";
            logger.LogError("OutOfMemoryException during conversion: {FileName}", item.File.FileName);
            return new ConvertImageOutOfMemory(itemId);
        }
        catch (Exception ex)
        {
            item.Status = ImageItemStatus.Error;
            item.ErrorMessage = $"转换失败：{ex.Message}";
            logger.LogError(ex, "Conversion failed for {FileName}", item.File.FileName);
            return new ConvertImageFailed(itemId, item.ErrorMessage);
        }
    }

    public async Task<ConvertAllResult> ConvertAllAsync(Action? progressChanged = null)
    {
        if (_batch.IsConverting)
            return new ConvertAllAlreadyRunning();

        var itemIds = _items
            .Where(CanConvert)
            .Select(i => i.Id)
            .ToArray();

        _batch = new BatchConversionSnapshot(true, 0, itemIds.Length);
        progressChanged?.Invoke();

        var convertedCount = 0;
        foreach (var itemId in itemIds)
        {
            await Task.Yield();
            await ConvertImageAsync(itemId);
            convertedCount++;
            _batch = _batch with { ConvertedCount = convertedCount };
            progressChanged?.Invoke();
        }

        _batch = _batch with { IsConverting = false };
        progressChanged?.Invoke();
        return new ConvertAllSucceeded(convertedCount);
    }

    public CreatePreviewResult CreatePreview(Guid itemId, int maxSize)
    {
        var item = Find(itemId);
        if (item is null)
            return new CreatePreviewItemNotFound(itemId);
        if (item.Job is not { IsLoaded: true })
            return new CreatePreviewNotLoaded(itemId);

        try
        {
            return new CreatePreviewSucceeded(
                itemId,
                previewBuilder.CreatePreview(item.Job, new ImagePreviewRequest(maxSize)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create preview for {FileName}", item.File.FileName);
            return new CreatePreviewFailed(itemId, ex.Message);
        }
    }

    public OpenConvertedImageResult OpenConvertedImage(Guid itemId)
    {
        var item = Find(itemId);
        if (item is null)
            return new OpenConvertedImageItemNotFound(itemId);
        if (item.Result is null)
            return new OpenConvertedImageNotReady(itemId);

        item.Result.Stream.Position = 0;
        return new OpenConvertedImageSucceeded(itemId, item.Result.OutputFileName, item.Result.Stream);
    }

    public async Task<BuildConvertedPackageResult> BuildConvertedPackageAsync()
    {
        var entries = _items
            .Where(i => i.Status is ImageItemStatus.Done)
            .Select(i => i.Result)
            .OfType<ConversionResult>()
            .Select(r => new ImagePackageEntrySource(r.OutputFileName, r.Stream))
            .ToList();

        var built = await packageBuilder.BuildAsync(entries, "converted-images.zip");
        if (built is BuildImagePackageSucceeded ok)
            return new BuildConvertedPackageSucceeded(ok.Package.FileName, ok.Package.Stream);
        return new BuildConvertedPackageEmpty();
    }

    public RemoveItemResult RemoveItem(Guid itemId)
    {
        var item = Find(itemId);
        if (item is null)
            return new RemoveItemNotFound(itemId);

        _items.Remove(item);
        item.Dispose();
        return new RemoveItemSucceeded(itemId);
    }

    public void Clear()
    {
        foreach (var item in _items)
            item.Dispose();
        _items.Clear();
    }

    private SessionImageItem? Find(Guid itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId);

    private static ImageItemSnapshot CreateSnapshot(SessionImageItem item) =>
        new(
            item.Id,
            item.File.FileName,
            item.File.FileSize,
            item.Job?.Width ?? 0,
            item.Job?.Height ?? 0,
            item.ThumbnailUrl,
            item.Status,
            item.ErrorMessage,
            item.Result?.Size,
            CanConvert(item),
            item.Status is ImageItemStatus.Done,
            true);

    private static bool CanConvert(SessionImageItem item) =>
        item.Job is { IsLoaded: true }
        && item.Status is ImageItemStatus.Pending or ImageItemStatus.Done or ImageItemStatus.Error;

    private sealed class SessionImageItem(BrowserImageFile file) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public BrowserImageFile File { get; } = file;
        public ImageItemStatus Status { get; set; } = ImageItemStatus.Loading;
        public ImageDocument? Job { get; set; }
        public ConversionResult? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ThumbnailUrl { get; set; }

        public void Dispose()
        {
            Result?.Stream.Dispose();
            Job?.Dispose();
        }
    }
}
