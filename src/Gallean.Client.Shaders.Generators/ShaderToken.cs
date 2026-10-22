using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Shaders.Generators;

internal sealed record ShaderToken(ShaderTokenType Type, string Text, TextSpan Span);
