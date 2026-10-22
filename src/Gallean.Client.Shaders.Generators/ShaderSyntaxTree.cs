using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Shaders.Generators;

internal sealed record ShaderSyntaxTree(IReadOnlyList<ShaderUniformDeclaration> Uniforms, IReadOnlyList<TextSpan> Errors);
