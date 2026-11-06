using System.Runtime.InteropServices;

namespace Gallean.Client.MacOS;

internal static partial class Api
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    public const nuint NSApplicationPresentationDefault = 0;
    public const nuint NSApplicationPresentationHideDock = 1 << 1;
    public const nuint NSApplicationPresentationHideMenuBar = 1 << 3;

    public const nuint NSWindowCollectionBehaviorFullScreenNone = 1 << 9;

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct CGRect(double X, double Y, double Width, double Height);

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct NSEdgeInsets(double Top, double Left, double Bottom, double Right);

    public static nint Selector(string name)
    {
        return sel_registerName(name);
    }

    public static nint SharedApplication()
    {
        return Send(objc_getClass("NSApplication"), Selector("sharedApplication"));
    }

    /// <summary>
    /// Sends a message that returns <see cref="NSEdgeInsets"/>, which needs objc_msgSend_stret on x64.
    /// </summary>
    public static NSEdgeInsets SendInsets(nint receiver, nint selector)
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? objc_msgSend_stret_insets(receiver, selector)
            : objc_msgSend_insets(receiver, selector);
    }

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial void Send(nint receiver, nint selector, nuint argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial nuint SendUInt(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SendBool(nint receiver, nint selector, nint argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    public static partial void Send(nint receiver, nint selector, CGRect rect, [MarshalAs(UnmanagedType.U1)] bool flag);

    /// <summary>
    /// Sends a message that returns a <see cref="CGRect"/>, which needs objc_msgSend_stret on x64.
    /// </summary>
    public static CGRect SendRect(nint receiver, nint selector)
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? objc_msgSend_stret_rect(receiver, selector)
            : objc_msgSend_rect(receiver, selector);
    }

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial CGRect objc_msgSend_rect(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend_stret")]
    private static partial CGRect objc_msgSend_stret_rect(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial NSEdgeInsets objc_msgSend_insets(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend_stret")]
    private static partial NSEdgeInsets objc_msgSend_stret_insets(nint receiver, nint selector);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint sel_registerName(string name);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_getClass(string name);
}
