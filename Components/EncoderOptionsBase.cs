using System.Reflection;
using ImageConverter.Services;
using Microsoft.AspNetCore.Components;
using SixLabors.ImageSharp.Formats;

namespace ImageConverter.Components;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FormatEncoderAttribute(Type formatType) : Attribute
{
    public Type FormatType { get; } = formatType;
}

public abstract class EncoderOptionsBase : ComponentBase
{
    [Parameter] public EventCallback<IEncoderConfig> EncoderConfigChanged { get; set; }

    protected Task NotifyConfigChanged(IEncoderConfig config) =>
        EncoderConfigChanged.InvokeAsync(config);

    private static readonly Dictionary<Type, Type> FormatComponentMap =
        typeof(EncoderOptionsBase).Assembly.GetTypes()
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<FormatEncoderAttribute>()))
            .Where(x => x.Attr is not null)
            .ToDictionary(x => x.Attr!.FormatType, x => x.Type);

    public static Type? GetComponentType(IImageFormat format) =>
        FormatComponentMap.TryGetValue(format.GetType(), out var type) ? type : null;
}
