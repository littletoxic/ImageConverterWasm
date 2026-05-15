using System.Reflection;
using ImageConverter.Models;
using Microsoft.AspNetCore.Components;
using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Components;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FormatEncoderAttribute(string formatId) : Attribute
{
    public FormatId FormatId { get; } = new(formatId);
}

public abstract class EncoderOptionsBase : ComponentBase
{
    [Parameter] public EventCallback<IImageEncoder> EncoderChanged { get; set; }
    [Parameter] public bool SkipMetadata { get; set; }

    protected abstract IImageEncoder BuildEncoder();

    protected Task NotifyEncoderChanged() =>
        EncoderChanged.InvokeAsync(BuildEncoder());

    private bool _initialized;
    private bool _lastSkipMetadata;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _initialized = true;
            _lastSkipMetadata = SkipMetadata;
            await NotifyEncoderChanged();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_initialized && SkipMetadata != _lastSkipMetadata)
        {
            _lastSkipMetadata = SkipMetadata;
            await NotifyEncoderChanged();
        }
    }

    private static readonly Dictionary<FormatId, Type> FormatComponentMap =
        typeof(EncoderOptionsBase).Assembly.GetTypes()
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<FormatEncoderAttribute>()))
            .Where(x => x.Attr is not null)
            .ToDictionary(x => x.Attr!.FormatId, x => x.Type);

    public static Type? GetComponentType(FormatId formatId) =>
        FormatComponentMap.TryGetValue(formatId, out var type) ? type : null;
}
