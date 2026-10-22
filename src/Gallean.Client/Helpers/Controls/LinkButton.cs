using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class LinkButton : HyperlinkButton
{
    protected override Type StyleKeyOverride => typeof(Button);
}
