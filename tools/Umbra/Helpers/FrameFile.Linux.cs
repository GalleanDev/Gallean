using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Umbra;

internal sealed partial class FrameFile
{
    private static partial class Linux
    {
        private static partial class Api
        {
            [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
            public static partial int memfd_create(string name, uint flags);
        }

        public const uint MFD_CLOEXEC = 1;

        public static SafeFileHandle memfd_create(string name, uint flags)
        {
            int descriptor = Api.memfd_create(name, flags);

            if (descriptor < 0)
            {
                string error = Marshal.GetLastPInvokeErrorMessage();

                throw new IOException($"Creating the file '{name}' failed: {error}.");
            }

            return new SafeFileHandle(descriptor, ownsHandle: true);
        }
    }
}
