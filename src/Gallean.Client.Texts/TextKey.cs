using System.Collections.Concurrent;
using System.ComponentModel;

namespace Gallean.Client.Texts;

public sealed class TextKey : INotifyPropertyChanged
{
    private static readonly ConcurrentDictionary<string, TextKey> keys = new(StringComparer.Ordinal);
    private static readonly PropertyChangedEventArgs translationChanged = new(nameof(Translation));

    private TextKey(string text)
    {
        Text = text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text { get; }

    public string Translation => TextCatalog.Current.Translate(Text);

    public static TextKey For(string text)
    {
        return keys.GetOrAdd(text, static text => new TextKey(text));
    }

    internal static void NotifyTranslationChanged()
    {
        foreach (TextKey key in keys.Values)
        {
            key.PropertyChanged?.Invoke(key, translationChanged);
        }
    }
}
