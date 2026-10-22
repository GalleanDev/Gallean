namespace Gallean.Tests.Common;

public readonly record struct PerformanceMeasurement(TimeSpan ElapsedTime, TimeSpan CpuTime, long AllocatedBytes);
