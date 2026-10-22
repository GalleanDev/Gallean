using System.Text;

internal sealed class CodeWriter
{
    private sealed class IndentBlock(CodeWriter writer, string closing) : IDisposable
    {
        public void Dispose()
        {
            writer.indentation--;
            writer.AppendLine(closing);
        }
    }

    private readonly StringBuilder sb = new();
    private int indentation;

    public void AppendLine(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            sb.AppendLine();
        }
        else
        {
            sb.Append(' ', indentation * 4);
            sb.AppendLine(text);
        }
    }

    public IDisposable Block(string? header = null, string closing = "}")
    {
        if (header is not null)
        {
            AppendLine(header);
        }

        AppendLine("{");
        indentation++;

        return new IndentBlock(this, closing);
    }

    public override string ToString() => sb.ToString();
}
