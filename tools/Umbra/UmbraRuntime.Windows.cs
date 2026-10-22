using System.Runtime.InteropServices;

namespace Umbra;

internal static partial class UmbraRuntime
{
    private static partial class Windows
    {
        private static partial class Api
        {
            [LibraryImport("ucrtbase", EntryPoint = "_putenv_s", StringMarshalling = StringMarshalling.Utf8)]
            public static partial int _putenv_s(string name, string value);
        }

        public static void SetEnvironmentVariable(string name, string value)
        {
            int result = Api._putenv_s(name, value);

            if (result != 0)
            {
                throw new InvalidOperationException($"Setting environment variable '{name}' failed with error '{result}'.");
            }
        }
    }
}
