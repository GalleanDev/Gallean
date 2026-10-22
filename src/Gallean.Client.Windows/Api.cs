using System.Runtime.InteropServices;

namespace Gallean.Client.Windows;

internal static partial class Api
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct RECT(int Left, int Top, int Right, int Bottom);

    public const uint WM_SIZE = 0x0005;
    public const uint WM_ERASEBKGND = 0x0014;
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

    [LibraryImport("user32")]
    public static partial int FillRect(IntPtr deviceContext, in RECT rect, IntPtr brush);

    [LibraryImport("gdi32")]
    public static partial IntPtr CreateSolidBrush(uint color);

    [LibraryImport("gdi32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(IntPtr gdiObject);
}
