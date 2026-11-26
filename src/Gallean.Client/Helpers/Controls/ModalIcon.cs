using Avalonia;
using Avalonia.Controls.Primitives;

namespace Gallean.Client;

internal sealed partial class ModalIcon : TemplatedControl
{
    [GeneratedStyledProperty]
    public partial IconMoniker Icon { get; set; }
}
