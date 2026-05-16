using ImageConverter.Models.Encoding;
using Microsoft.AspNetCore.Components;

namespace ImageConverter.Components;

public abstract class EncoderOptionsBase : ComponentBase
{
    [Parameter] public EventCallback<EncoderSettings> EncoderSettingsChanged { get; set; }

    protected abstract EncoderSettings BuildSettings();

    protected Task NotifySettingsChanged() =>
        EncoderSettingsChanged.InvokeAsync(BuildSettings());
}
