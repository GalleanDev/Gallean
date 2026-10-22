namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private sealed class Part<TInput, TValue>(Func<TInput, TValue> buildFunc)
        where TInput : struct, IEquatable<TInput>
        where TValue : class
    {
        private TInput cachedInput;
        private TValue? cachedValue;

        public TValue Resolve(TInput input)
        {
            if (cachedValue is not null && cachedInput.Equals(input))
            {
                return cachedValue;
            }

            TValue value = buildFunc(input);

            cachedInput = input;
            cachedValue = value;

            return value;
        }
    }
}
