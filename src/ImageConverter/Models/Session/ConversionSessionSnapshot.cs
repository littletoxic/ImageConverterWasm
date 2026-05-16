using ImageConverter.Models.Formats;

namespace ImageConverter.Models.Session;

public sealed record ConversionSessionSnapshot(
    IReadOnlyList<ImageItemSnapshot> Items,
    FormatId TargetFormatId,
    BatchConversionSnapshot Batch,
    bool CanConvertAll,
    bool CanDownloadAll);

public sealed record BatchConversionSnapshot(
    bool IsConverting,
    int ConvertedCount,
    int TotalCount);

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
