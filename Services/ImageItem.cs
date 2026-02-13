using Microsoft.AspNetCore.Components.Forms;

namespace ImageConverter.Services;

public enum ImageItemStatus
{
    Loading,
    Pending,
    Converting,
    Done,
    Error
}

public sealed class ImageItem(IBrowserFile file) : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public IBrowserFile BrowserFile { get; } = file;
    public string FileName => BrowserFile.Name;
    public long FileSize => BrowserFile.Size;
    public ImageItemStatus Status { get; set; } = ImageItemStatus.Loading;
    public LoadedImage? Job { get; set; }
    public ConversionResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ThumbnailUrl { get; set; }

    public void Dispose()
    {
        Result?.Stream.Dispose();
        Job?.Dispose();
    }
}
