using System.Diagnostics;

namespace Gallean.Tests.Common;

public readonly struct PerformanceTimer()
{
    private readonly long startAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
    private readonly TimeSpan startCpuTime = Environment.CpuUsage.TotalTime;
    private readonly long startTimestamp = Stopwatch.GetTimestamp();

    public PerformanceMeasurement Stop(int iterations)
    {
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        TimeSpan cpuTime = Environment.CpuUsage.TotalTime - startCpuTime;
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - startAllocatedBytes;

        return new PerformanceMeasurement(elapsed / iterations, cpuTime / iterations, allocatedBytes / iterations);
    }
}
