namespace Gallean.Client;

internal sealed partial class DiagnosticsRecorder
{
    private sealed class TimingAccumulator
    {
        private TimeSpan last;
        private TimeSpan total;
        private TimeSpan maximum;
        private long count;

        public void Add(TimeSpan value)
        {
            last = value;
            total += value;
            count++;

            if (value > maximum)
            {
                maximum = value;
            }
        }

        public DiagnosticsReport.Timing Capture()
        {
            if (count > 0)
            {
                return new DiagnosticsReport.Timing(last, total / count, maximum);
            }
            else
            {
                return new DiagnosticsReport.Timing(last, TimeSpan.Zero, maximum);
            }
        }

        public void Reset()
        {
            total = TimeSpan.Zero;
            maximum = TimeSpan.Zero;
            count = 0;
        }
    }
}
