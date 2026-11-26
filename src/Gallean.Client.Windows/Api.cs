using System.Runtime.InteropServices;

namespace Gallean.Client.Windows;

internal static partial class Api
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct RECT(int Left, int Top, int Right, int Bottom);

    public const uint WM_SIZE = 0x0005;
    public const nuint SIZE_RESTORED = 0;

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ClipCursor(in RECT rect);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ClipCursor(IntPtr rect);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClipCursor(out RECT rect);

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(IntPtr window, out RECT rect);

    [LibraryImport("user32", EntryPoint = "SendMessageW")]
    public static partial IntPtr SendMessage(IntPtr window, uint message, nuint wParam, nint lParam);
}
