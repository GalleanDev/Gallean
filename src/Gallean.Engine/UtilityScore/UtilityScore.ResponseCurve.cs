namespace Gallean.Engine;

internal static partial class UtilityScore
{
    public abstract record ResponseCurve
    {
        public sealed record Polynomial(int Slope, int Exponent, int XShift, int YShift) : ResponseCurve
        {
            protected override int EvaluateCore(int input)
            {
                int shifted = input - XShift;
                int powered = Power(shifted, Exponent);

                return Slope * powered / MaxScore + YShift;
            }
        }

        public sealed record Root(int Slope, int Exponent, int XShift, int YShift) : ResponseCurve
        {
            protected override int EvaluateCore(int input)
            {
                int shifted = Math.Clamp(input - XShift, 0, MaxScore);
                int rooted = InversePower(shifted, Exponent);

                return Slope * rooted / MaxScore + YShift;
            }
        }

        public sealed record Threshold(int Point, int LowScore, int HighScore) : ResponseCurve
        {
            protected override int EvaluateCore(int input)
            {
                if (input < Point)
                {
                    return LowScore;
                }
                else
                {
                    return HighScore;
                }
            }
        }

        public sealed record Logistic(int Slope, int Midpoint, int YShift) : ResponseCurve
        {
            protected override int EvaluateCore(int input)
            {
                int scaled = Slope * (input - Midpoint) / MaxScore;
                int sigmoid = MaxScore / 2 + MaxScore * scaled / (2 * (MaxScore + Math.Abs(scaled)));

                return YShift + (MaxScore - YShift) * sigmoid / MaxScore;
            }
        }

        public sealed record Logit(int Slope, int Midpoint) : ResponseCurve
        {
            protected override int EvaluateCore(int input)
            {
                int offset = (input - Midpoint) * 2;

                if (offset >= MaxScore)
                {
                    return MaxScore;
                }

                if (offset <= -MaxScore)
                {
                    return 0;
                }

                int scaled = MaxScore * offset / (MaxScore - Math.Abs(offset));

                return MaxScore / 2 + Slope * scaled / (2 * MaxScore);
            }
        }

        public sealed record Bell(int Center, int Width, int Height, int YShift) : ResponseCurve
        {
            protected override int EvaluateCore(int input)
            {
                int distance = Math.Abs(input - Center);

                if (Width <= 0 || distance >= Width)
                {
                    return YShift;
                }

                int normalized = MaxScore * distance / Width;
                int falloff = MaxScore - normalized * normalized / MaxScore;

                return YShift + Height * falloff / MaxScore;
            }
        }

        public static ResponseCurve Linear { get; } = new Polynomial(Slope: MaxScore, Exponent: 1, XShift: 0, YShift: 0);

        public int Evaluate(int input)
        {
            return Math.Clamp(EvaluateCore(input), 0, MaxScore);
        }

        protected abstract int EvaluateCore(int input);

        private static int Power(int value, int exponent)
        {
            int result = MaxScore;

            for (int index = 0; index < exponent; index++)
            {
                result = result * value / MaxScore;
            }

            return result;
        }

        private static int InversePower(int value, int exponent)
        {
            long target = value;

            for (int index = 1; index < exponent; index++)
            {
                target *= MaxScore;
            }

            int low = 0;
            int high = MaxScore;

            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                long powered = 1;

                for (int index = 0; index < exponent; index++)
                {
                    powered *= middle;
                }

                if (powered <= target)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }
    }
}
