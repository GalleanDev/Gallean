using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Gallean.Client;

internal sealed class HotkeyLabel : Decorator
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HotkeyLabel, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string> HotkeyProperty =
        AvaloniaProperty.Register<HotkeyLabel, string>(nameof(Hotkey), string.Empty);

    private readonly TextBlock label = new();

    public HotkeyLabel()
    {
        Child = label;
        UpdateInlines();
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

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
