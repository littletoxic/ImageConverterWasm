namespace ImageConverter.Models;

public sealed record BrowserImageFile(
    string FileName,
    long FileSize,
    Func<long, Stream> OpenReadStream);
