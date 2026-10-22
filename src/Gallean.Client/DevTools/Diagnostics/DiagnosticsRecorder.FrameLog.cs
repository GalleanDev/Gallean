namespace Gallean.Client;

internal sealed partial class DiagnosticsRecorder
{
    private sealed class FrameLog
    {
        private const int BudgetSampleMinimum = 8;
        private const int BudgetPercentileDivisor = 8;

        private static readonly TimeSpan MinimumBudget = TimeSpan.FromSeconds(1.0 / 165.0);
        private static readonly TimeSpan MaximumBudget = TimeSpan.FromSeconds(1.0 / 58.0);

        private readonly Frame[] frames = new Frame[FrameCapacity];
        private int count;
        private int head;

        public void Add(Frame frame)
        {
            frames[head] = frame;
            head = (head + 1) % FrameCapacity;

            if (count < FrameCapacity)
            {
                count++;
            }
        }

        public int CopyTo(Span<Frame> destination)
        {
            int oldest = (head - count + FrameCapacity) % FrameCapacity;

            for (int index = 0; index < count; index++)
            {
                destination[index] = frames[(oldest + index) % FrameCapacity];
            }

            return count;
        }

        public TimeSpan EstimateBudget(TimeSpan current)
        {
            Span<Frame> ordered = stackalloc Frame[FrameCapacity];
            int total = CopyTo(ordered);

            Span<long> continuousTicks = stackalloc long[FrameCapacity];
            int continuousCount = 0;

            for (int index = 0; index < total; index++)
            {
                Frame frame = ordered[index];

                if (frame.Continuous)
                {
                    continuousTicks[continuousCount] = frame.Interval.Ticks;
                    continuousCount++;
                }
            }

            if (continuousCount < BudgetSampleMinimum)
            {
                return current;
            }

            continuousTicks[..continuousCount].Sort();

            long floorTicks = continuousTicks[continuousCount / BudgetPercentileDivisor];

            return TimeSpan.FromTicks(Math.Clamp(floorTicks, MinimumBudget.Ticks, MaximumBudget.Ticks));
        }
    }
}
