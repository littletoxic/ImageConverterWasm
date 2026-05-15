namespace ImageConverter.Models;

public sealed record ConversionSessionSnapshot(
    IReadOnlyList<ImageItemSnapshot> Items,
    FormatId TargetFormatId,
    bool CanConvertAll,
    bool CanDownloadAll);

public sealed record ImageItemSnapshot(
    Guid Id,
    string FileName,
    long FileSize,
    int Width,
    int Height,
    string? ThumbnailUrl,
    ImageItemStatus Status,
    string? ErrorMessage,
    long? ResultSize,
    bool CanConvert,
    bool CanDownload,
    bool CanRemove);
