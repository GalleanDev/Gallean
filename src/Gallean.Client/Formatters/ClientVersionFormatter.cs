namespace Gallean.Client;

internal static class ClientVersionFormatter
{
    public static string Format(ClientVersion version)
    {
        if (version.Revision is not null)
        {
            int length = Math.Min(version.Revision.Length, 9);

            return string.Join('+', version.Version, version.Revision[..length]);
        }
        else
        {
            return version.Version;
        }
    }
}
