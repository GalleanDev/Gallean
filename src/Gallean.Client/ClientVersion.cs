using System.Reflection;

namespace Gallean.Client;

internal sealed record ClientVersion(string Version, string? Revision) : IComparable<ClientVersion>
{
    public static ClientVersion Get()
    {
        Assembly assembly = typeof(ClientVersion).Assembly;
        AssemblyInformationalVersionAttribute? attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (attribute is null)
        {
            throw new InvalidOperationException("Unexpected client assembly version.");
        }

        return Deserialize(attribute.InformationalVersion);
    }

    public static ClientVersion Deserialize(string value)
    {
        int separator = value.IndexOf('+');

        if (separator >= 0)
        {
            return new ClientVersion(value[..separator], value[(separator + 1)..]);
        }
        else
        {
            return new ClientVersion(value, null);
        }
    }

    public string Serialize()
    {
        if (Revision is not null)
        {
            return $"{Version}+{Revision}";
        }
        else
        {
            return Version;
        }
    }

    public int CompareTo(ClientVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        Version? version = TryParse(Version);
        Version? otherVersion = TryParse(other.Version);

        return Comparer<Version?>.Default.Compare(version, otherVersion);

        static Version? TryParse(string value)
        {
            return System.Version.TryParse(value, out Version? number) ? number : null;
        }
    }

    public static bool operator <(ClientVersion left, ClientVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(ClientVersion left, ClientVersion right)
    {
        return left.CompareTo(right) > 0;
    }
}
