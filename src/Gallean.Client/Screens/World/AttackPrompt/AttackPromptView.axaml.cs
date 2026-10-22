using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class AttackPromptView : UserControl
{
    public AttackPromptView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
