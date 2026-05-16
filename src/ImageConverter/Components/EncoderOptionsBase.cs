using System.Reflection;
using ImageConverter.Models.Encoding;
using ImageConverter.Models.Formats;
using Microsoft.AspNetCore.Components;

namespace ImageConverter.Components;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FormatEncoderAttribute(string formatId) : Attribute
{
    public FormatId FormatId { get; } = new(formatId);
}

public abstract class EncoderOptionsBase : ComponentBase
{
    [Parameter] public EventCallback<EncoderSettings> EncoderSettingsChanged { get; set; }
    [Parameter] public bool SkipMetadata { get; set; }

    protected abstract EncoderSettings BuildSettings();

    protected Task NotifySettingsChanged() =>
        EncoderSettingsChanged.InvokeAsync(BuildSettings());

    private bool _initialized;
    private bool _lastSkipMetadata;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _initialized = true;
            _lastSkipMetadata = SkipMetadata;
            await NotifySettingsChanged();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_initialized && SkipMetadata != _lastSkipMetadata)
        {
            _lastSkipMetadata = SkipMetadata;
            await NotifySettingsChanged();
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
