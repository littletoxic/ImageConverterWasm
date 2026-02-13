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

public sealed class ImageItem : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public IBrowserFile BrowserFile { get; }
    public string FileName => BrowserFile.Name;
    public long FileSize => BrowserFile.Size;
    public ImageItemStatus Status { get; set; } = ImageItemStatus.Loading;
    public ImageConversionJob? Job { get; set; }
    public ConversionResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ThumbnailUrl { get; set; }

    public ImageItem(IBrowserFile file)
    {
        BrowserFile = file;
    }

    public void Dispose()
    {
        Result?.Stream.Dispose();
        Job?.Dispose();
    }
}
