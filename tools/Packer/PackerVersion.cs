using System.Reflection;

namespace Packer;

internal static class PackerVersion
{
    public static string Get()
    {
        Assembly assembly = typeof(PackerVersion).Assembly;
        AssemblyInformationalVersionAttribute? attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (attribute is null)
        {
            throw new InvalidOperationException("Unexpected packer assembly version.");
        }

        return attribute.InformationalVersion;
    }
}
