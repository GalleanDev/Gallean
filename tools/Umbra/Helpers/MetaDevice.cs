using TorchSharp;

namespace Umbra;

internal static class MetaDevice
{
    public static T Construct<T>(Func<T> factory)
    {
        torch.Device device = torch.get_default_device();

        torch.set_default_device(new torch.Device("meta"));

        try
        {
            return factory();
        }
        finally
        {
            torch.set_default_device(device);
        }
    }
}
