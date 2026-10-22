using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Umbra;

internal sealed partial class FrameFile
{
    private static partial class Windows
    {
        private static partial class Api
        {
            [LibraryImport("kernel32", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
            public static partial SafeFileHandle CreateFileW(
                string fileName,
                uint desiredAccess,
                uint shareMode,
                IntPtr securityAttributes,
                uint creationDisposition,
                uint flagsAndAttributes,
                IntPtr templateFile);
        }

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint CREATE_NEW = 1;
        public const uint FILE_ATTRIBUTE_TEMPORARY = 0x100;
        public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;

        public static SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            uint creationDisposition,
            uint flagsAndAttributes)
        {
            SafeFileHandle handle = Api.CreateFileW(
                fileName,
                desiredAccess,
                shareMode,
                IntPtr.Zero,
                creationDisposition,
                flagsAndAttributes,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                string error = Marshal.GetLastPInvokeErrorMessage();

                throw new IOException($"Creating the file '{fileName}' failed: {error}.");
            }

            return handle;
        }
    }
}
