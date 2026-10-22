using System.Runtime.InteropServices;
using SkiaSharp;

namespace Gallean.Client.Shaders;

internal sealed class ShaderProgram(ShaderKey key)
{
    public readonly ref struct Editor
    {
        private readonly SKRuntimeShaderBuilder builder;
        private readonly Lock locker;

        internal Editor(SKRuntimeShaderBuilder builder, Lock locker)
        {
            this.builder = builder;
            this.locker = locker;

            locker.Enter();
        }

        public void SetChild(string name, SKShader child)
        {
            builder.Children[name] = child;
        }

        public void SetUniform(string name, float value)
        {
            builder.Uniforms[name] = value;
        }

        public void SetUniform(string name, SKPoint value)
        {
            builder.Uniforms[name] = value;
        }

        public void SetUniform(string name, SKPoint3 value)
        {
            builder.Uniforms[name] = value;
        }

        public void SetUniform(string name, SKColorF value)
        {
            builder.Uniforms[name] = value;
        }

        public void SetUniform(string name, ReadOnlySpan<float> value)
        {
            builder.Uniforms[name] = value;
        }

        public void SetUniform(string name, ReadOnlySpan<SKPoint> value)
        {
            builder.Uniforms[name] = MemoryMarshal.Cast<SKPoint, float>(value);
        }

        public void SetUniform(string name, ReadOnlySpan<SKPoint3> value)
        {
            builder.Uniforms[name] = MemoryMarshal.Cast<SKPoint3, float>(value);
        }

        public void SetUniform(string name, ReadOnlySpan<SKColorF> value)
        {
            builder.Uniforms[name] = MemoryMarshal.Cast<SKColorF, float>(value);
        }

        public SKShader Build()
        {
            return builder.Build();
        }

        public SKData ToUniformData()
        {
            return builder.Uniforms.ToData();
        }

        public void Dispose()
        {
            locker.Exit();
        }
    }

    private readonly SKRuntimeShaderBuilder builder = new(ShaderLibrary.Compile(key));
    private readonly Lock locker = new();

    public Editor Edit()
    {
        return new Editor(builder, locker);
    }
}
