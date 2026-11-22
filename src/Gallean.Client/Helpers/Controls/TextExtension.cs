using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed class TextExtension(string text)
{
    private static readonly FuncValueConverter<string, TextSpans> spansConverter = new(value => TextSpans.FromText(value ?? string.Empty));

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        TextKey source = TextKey.For(text);

        if (IsSpansTarget(serviceProvider))
        {
            return CompiledBinding.Create<TextKey, string>(key => key.Translation, source, converter: spansConverter);
        }
        else
        {
            return CompiledBinding.Create<TextKey, string>(key => key.Translation, source);
        }
    }

    private static bool IsSpansTarget(IServiceProvider serviceProvider)
    {
        IProvideValueTarget? target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        return target?.TargetProperty is AvaloniaProperty property && property.PropertyType == typeof(TextSpans);
    }
}
