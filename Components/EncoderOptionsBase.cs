using System.Reflection;
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
    [Parameter] public EventCallback<IImageEncoder> EncoderChanged { get; set; }

    protected abstract IImageEncoder BuildEncoder();

    protected Task NotifyEncoderChanged() =>
        EncoderChanged.InvokeAsync(BuildEncoder());

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await NotifyEncoderChanged();
    }

    private static readonly Dictionary<Type, Type> FormatComponentMap =
        typeof(EncoderOptionsBase).Assembly.GetTypes()
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<FormatEncoderAttribute>()))
            .Where(x => x.Attr is not null)
            .ToDictionary(x => x.Attr!.FormatType, x => x.Type);

    public static Type? GetComponentType(IImageFormat format) =>
        FormatComponentMap.TryGetValue(format.GetType(), out var type) ? type : null;
}
