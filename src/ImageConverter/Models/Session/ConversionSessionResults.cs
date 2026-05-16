namespace ImageConverter.Models.Session;

public sealed record AddFilesSucceeded(IReadOnlyList<Guid> ItemIds);
public union AddFilesResult(AddFilesSucceeded);

public sealed record LoadImageSucceeded(Guid ItemId);
public sealed record LoadImageItemNotFound(Guid ItemId);
public sealed record LoadImageFailed(Guid ItemId, string Message);
public union LoadImageResult(
    LoadImageSucceeded,
    LoadImageItemNotFound,
    LoadImageFailed);

public sealed record ConvertImageSucceeded(Guid ItemId);
public sealed record ConvertImageItemNotFound(Guid ItemId);
public sealed record ConvertImageNotLoaded(Guid ItemId);
public sealed record ConvertImageOutOfMemory(Guid ItemId);
public sealed record ConvertImageFailed(Guid ItemId, string Message);
public union ConvertImageResult(
    ConvertImageSucceeded,
    ConvertImageItemNotFound,
    ConvertImageNotLoaded,
    ConvertImageOutOfMemory,
    ConvertImageFailed);

public sealed record ConvertAllSucceeded(int ConvertedCount);
public sealed record ConvertAllAlreadyRunning;
public union ConvertAllResult(
    ConvertAllSucceeded,
    ConvertAllAlreadyRunning);

public sealed record CreatePreviewSucceeded(Guid ItemId, string DataUrl);
public sealed record CreatePreviewItemNotFound(Guid ItemId);
public sealed record CreatePreviewNotLoaded(Guid ItemId);
public sealed record CreatePreviewFailed(Guid ItemId, string Message);
public union CreatePreviewResult(
    CreatePreviewSucceeded,
    CreatePreviewItemNotFound,
    CreatePreviewNotLoaded,
    CreatePreviewFailed);

public sealed record OpenConvertedImageSucceeded(Guid ItemId, string OutputFileName, MemoryStream Stream);
public sealed record OpenConvertedImageItemNotFound(Guid ItemId);
public sealed record OpenConvertedImageNotReady(Guid ItemId);
public union OpenConvertedImageResult(
    OpenConvertedImageSucceeded,
    OpenConvertedImageItemNotFound,
    OpenConvertedImageNotReady);

public sealed record RemoveItemSucceeded(Guid ItemId);
public sealed record RemoveItemNotFound(Guid ItemId);
public union RemoveItemResult(
    RemoveItemSucceeded,
    RemoveItemNotFound);

public sealed record BuildConvertedPackageSucceeded(string FileName, MemoryStream Stream);
public sealed record BuildConvertedPackageEmpty;
public union BuildConvertedPackageResult(
    BuildConvertedPackageSucceeded,
    BuildConvertedPackageEmpty);
