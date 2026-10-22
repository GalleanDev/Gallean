using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Shaders.Generators;

internal sealed class ShaderParser(IReadOnlyList<ShaderToken> tokens)
{
    private readonly List<ShaderUniformDeclaration> uniforms = [];
    private readonly List<TextSpan> errors = [];
    private int position;
    private bool hasError;

    private ShaderToken Current => tokens[position];
    private ShaderToken Previous => tokens[position - 1];

    public ShaderSyntaxTree Parse()
    {
        while (Current.Type != ShaderTokenType.End)
        {
            if (Current.Type == ShaderTokenType.UniformKeyword)
            {
                ParseUniform();
            }
            else
            {
                Advance();
            }
        }

        return new ShaderSyntaxTree(uniforms, errors);
    }

    private void ParseUniform()
    {
        hasError = false;

        ShaderToken keyword = Expect(ShaderTokenType.UniformKeyword);
        ShaderToken type = Expect(ShaderTokenType.Identifier);
        ShaderToken name = Expect(ShaderTokenType.Identifier);
        int? length = ParseLength();
        ExpectEnd();

        TextSpan span = TextSpan.FromBounds(keyword.Span.Start, Previous.Span.End);

        if (hasError)
        {
            errors.Add(span);
        }
        else
        {
            uniforms.Add(new ShaderUniformDeclaration(type.Text, name.Text, length, span));
        }
    }

    private int? ParseLength()
    {
        if (Current.Type == ShaderTokenType.OpenBracket)
        {
            Advance();

            ShaderToken number = Expect(ShaderTokenType.Number);
            Expect(ShaderTokenType.CloseBracket);

            return GetLength(number);
        }
        else
        {
            return null;
        }
    }

    private int? GetLength(ShaderToken number)
    {
        if (int.TryParse(number.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int length))
        {
            return length;
        }
        else
        {
            hasError = true;

            return null;
        }
    }

    private void ExpectEnd()
    {
        while (Current.Type != ShaderTokenType.Semicolon &&
               Current.Type != ShaderTokenType.UniformKeyword &&
               Current.Type != ShaderTokenType.End)
        {
            hasError = true;
            Advance();
        }

        Expect(ShaderTokenType.Semicolon);
    }

    private ShaderToken Expect(ShaderTokenType type)
    {
        if (Current.Type == type)
        {
            return Advance();
        }
        else
        {
            hasError = true;

            return new ShaderToken(type, string.Empty, new TextSpan(Current.Span.Start, 0));
        }
    }

    private ShaderToken Advance()
    {
        ShaderToken token = Current;
        position++;

        return token;
    }
}
