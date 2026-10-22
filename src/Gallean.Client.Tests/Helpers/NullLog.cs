using Gallean.Common;

namespace Gallean.Client.Tests;

internal sealed class NullLog : ILog, IStaticInstance<NullLog>
{
    public string? Location => null;

    public void Write(string text)
    {
    }
}
