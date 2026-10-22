using System.Text;

namespace Gallean.Common;

public sealed class JoinStringBuilder(string separator)
{
    private readonly StringBuilder sb = new();

    public int Count { get; private set; }

    public JoinStringBuilder Add(string value)
    {
        if (Count > 0)
        {
            sb.Append(separator);
        }

        sb.Append(value);
        Count++;

        return this;
    }

    public JoinStringBuilder AddRange(IEnumerable<string> values)
    {
        foreach (string value in values)
        {
            Add(value);
        }

        return this;
    }

    public override string ToString()
    {
        return sb.ToString();
    }
}
