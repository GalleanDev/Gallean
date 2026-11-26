using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class AudioScope : AvaloniaObject
{
    [GeneratedAttachedProperty(Inherits = true)]
    public static partial IAudioPlayer? GetAudio(Control element);

    public static IAudioPlayer GetRequiredAudio(Control element)
    {
        IAudioPlayer? audio = GetAudio(element);

        if (audio is null)
        {
            throw new KeyNotFoundException("Audio scope is not set.");
        }

        return audio;
    }
}
