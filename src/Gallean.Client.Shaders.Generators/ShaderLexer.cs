using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Shaders.Generators;

internal sealed class ShaderLexer(string text)
{
    private const string UniformKeyword = "uniform";

    private int position;

    public IReadOnlyList<ShaderToken> ReadTokens()
    {
        List<ShaderToken> tokens = [];
        ShaderToken token;

        do
        {
            token = ReadToken();
            tokens.Add(token);
        } while (token.Type != ShaderTokenType.End);

        return tokens;
    }

    private ShaderToken ReadToken()
    {
        SkipTrivia();

        int start = position;

        if (position == text.Length)
        {
            return CreateToken(ShaderTokenType.End, start);
        }
        else if (IsIdentifierStart(text[position]))
        {
            SkipWhile(IsIdentifierPart);

            return CreateToken(GetWordType(start), start);
        }
        else if (char.IsDigit(text[position]))
        {
            SkipWhile(IsNumberPart);

            return CreateToken(ShaderTokenType.Number, start);
        }
        else
        {
            position++;

            return CreateToken(GetSymbolType(text[start]), start);
        }
    }

    private void SkipTrivia()
    {
        int start;

        do
        {
            start = position;

            SkipWhile(char.IsWhiteSpace);
            SkipLineComment();
            SkipBlockComment();
        } while (position != start);
    }

    private void SkipLineComment()
    {
        if (IsAt("//"))
        {
            SkipUntil(IsLineBreak);
        }
    }

    private void SkipBlockComment()
    {
        if (IsAt("/*"))
        {
            int end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);

            position = end < 0 ? text.Length : end + 2;
        }
    }

    private void SkipWhile(Func<char, bool> predicate)
    {
        while (position < text.Length && predicate(text[position]))
        {
            position++;
        }
    }

    private void SkipUntil(Func<char, bool> predicate)
    {
        while (position < text.Length && !predicate(text[position]))
        {
            position++;
        }
    }

    private bool IsAt(string value)
    {
        return string.CompareOrdinal(text, position, value, 0, value.Length) == 0;
    }

    private ShaderTokenType GetWordType(int start)
    {
        string word = text.Substring(start, position - start);

        if (word == UniformKeyword)
        {
            return ShaderTokenType.UniformKeyword;
        }
        else
        {
            return ShaderTokenType.Identifier;
        }
    }

    private ShaderToken CreateToken(ShaderTokenType type, int start)
    {
        return new ShaderToken(type, text.Substring(start, position - start), TextSpan.FromBounds(start, position));
    }

    private static ShaderTokenType GetSymbolType(char ch)
    {
        return ch switch
        {
            '[' => ShaderTokenType.OpenBracket,
            ']' => ShaderTokenType.CloseBracket,
            ';' => ShaderTokenType.Semicolon,
            _ => ShaderTokenType.Symbol
        };
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static bool IsNumberPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '.';
    }

    private static bool IsLineBreak(char ch)
    {
        return ch is '\r' or '\n';
    }
}
