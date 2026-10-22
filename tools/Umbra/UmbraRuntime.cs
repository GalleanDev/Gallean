using TorchSharp;

namespace Umbra;

internal static partial class UmbraRuntime
{
    public static void UseDevice(UmbraDevice device)
    {
        ConfigureAllocator();

        torch.set_default_device(GetDevice(device));
    }

    private static void ConfigureAllocator()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYTORCH_ALLOC_CONF")))
        {
            UpdateEnvironmentVariable("PYTORCH_ALLOC_CONF", "expandable_segments:True");
        }
    }

    public static torch.Device GetDevice(UmbraDevice device)
    {
        return device switch
        {
            UmbraDevice.Cpu => torch.CPU,
            UmbraDevice.Gpu => ResolveGpuDevice()
        };
    }

    private static torch.Device ResolveGpuDevice()
    {
        if (!torch.cuda_is_available())
        {
            throw new NotSupportedException("CUDA is not available.");
        }

        return torch.CUDA;
    }

    private static void UpdateEnvironmentVariable(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);

        if (OperatingSystem.IsWindows())
        {
            Windows.SetEnvironmentVariable(name, value);
        }
        else if (OperatingSystem.IsLinux())
        {
            Linux.SetEnvironmentVariable(name, value);
        }
    }
}
