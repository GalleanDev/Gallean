using System.Runtime.InteropServices;

namespace Umbra;

internal static partial class UmbraRuntime
{
    private static partial class Linux
    {
        private static partial class Api
        {
            [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
            public static partial int setenv(string name, string value, int overwrite);
        }

        private const int Overwrite = 1;

        public static void SetEnvironmentVariable(string name, string value)
        {
            if (Api.setenv(name, value, Overwrite) != 0)
            {
                string error = Marshal.GetLastPInvokeErrorMessage();

                throw new InvalidOperationException($"Setting environment variable '{name}' failed: {error}.");
            }
        }
    }
}
