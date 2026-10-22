using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;

namespace Gallean.Client;

internal static partial class AvaloniaExtensions
{
    private static class Api
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PresentationSource")]
        [return: UnsafeAccessorType("Avalonia.Controls.PresentationSource, Avalonia.Controls")]
        public static extern object GetPresentationSource(TopLevel topLevel);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetCursorOverride")]
        public static extern void SetCursorOverride([UnsafeAccessorType("Avalonia.Controls.PresentationSource, Avalonia.Controls")] object source, Cursor? cursor);
    }

    public static void SetCursorOverride(this TopLevel topLevel, Cursor? cursor)
    {
        object source = Api.GetPresentationSource(topLevel);

        Api.SetCursorOverride(source, cursor);
    }
}
