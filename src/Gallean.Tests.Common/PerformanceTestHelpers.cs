namespace Gallean.Tests.Common;

public static class PerformanceTestHelpers
{
    public static PerformanceMeasurement Measure(Action action, int iterations, int warmupIterations)
    {
        Run(action, warmupIterations);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        PerformanceTimer timer = new();

        Run(action, iterations);

        return timer.Stop(iterations);
    }

    private static void Run(Action action, int iterations)
    {
        for (int index = 0; index < iterations; index++)
        {
            action();
        }
    }
}
