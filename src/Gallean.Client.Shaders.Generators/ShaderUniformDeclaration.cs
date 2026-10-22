using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Shaders.Generators;

internal sealed record ShaderUniformDeclaration(string TypeName, string Name, int? Length, TextSpan Span);
