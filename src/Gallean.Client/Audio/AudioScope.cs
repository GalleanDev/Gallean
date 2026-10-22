using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class AudioScope : AvaloniaObject
{
    public static readonly AttachedProperty<IAudioPlayer?> AudioProperty =
        AvaloniaProperty.RegisterAttached<AudioScope, Control, IAudioPlayer?>("Audio", inherits: true);

    public static void SetAudio(Control element, IAudioPlayer? value)
    {
        element.SetValue(AudioProperty, value);
    }

    public static IAudioPlayer GetRequiredAudio(Control element)
    {
        IAudioPlayer? audio = element.GetValue(AudioProperty);

        if (audio is null)
        {
            throw new KeyNotFoundException("Audio scope is not set.");
        }

        return audio;
    }
}
