using System.Numerics;

namespace Gallean.Common;

public static class NumberExtensions
{
    extension<T>(T self) where T : INumber<T>
    {
        public T Mod(T divisor)
        {
            return ((self % divisor) + divisor) % divisor;
        }
    }
}
