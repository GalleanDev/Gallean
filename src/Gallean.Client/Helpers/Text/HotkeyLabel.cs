using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Gallean.Client;

internal sealed partial class HotkeyLabel : Decorator
{
    private readonly TextBlock label = new();

    public HotkeyLabel()
    {
        Child = label;
        UpdateInlines();
    }

    [GeneratedStyledProperty(DefaultValue = "")]
    public partial string Text { get; set; }

    [GeneratedStyledProperty(DefaultValue = "")]
    public partial string Hotkey { get; set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == TextProperty || e.Property == HotkeyProperty)
        {
            UpdateInlines();
        }
    }

    private void UpdateInlines()
    {
        int index = Text.IndexOf(Hotkey, StringComparison.CurrentCultureIgnoreCase);

        if (index >= 0)
        {
            int end = index + Hotkey.Length;

            label.Inlines =
            [
                new Run(Text[..index]),
                new Run(Text[index..end]) { Classes = { "hotkey" } },
                new Run(Text[end..])
            ];
        }
        else
        {
            label.Inlines = [new Run(Text)];
        }
    }
}
