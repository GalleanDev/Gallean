namespace Packer.Tests;

public sealed class ParallelBatchTests
{
    [Test]
    public void OrderTest()
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        List<(int Item, int Prepared)> results = [.. ParallelBatch.Prepare([0, 1, 2, 3, 4, 5, 6], parallelOptions, _ => 1, 3, item => item * 10)];

        Assert.That(results, Is.EqualTo([(0, 0), (1, 10), (2, 20), (3, 30), (4, 40), (5, 50), (6, 60)]));
    }

    [Test]
    public void CostBoundTest()
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        List<string> log = [];

        int Prepare(int item)
        {
            lock (log)
            {
                log.Add("prepare");
            }

            return item;
        }

        foreach ((int _, int prepared) in ParallelBatch.Prepare([1, 1, 1, 1], parallelOptions, item => item, 2, Prepare))
        {
            log.Add($"consume {prepared}");
        }

        Assert.That(log, Is.EqualTo([
            "prepare", "prepare", "consume 1", "consume 1",
            "prepare", "prepare", "consume 1", "consume 1"
        ]));
    }

    [Test]
    public void OversizedItemTest()
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        List<string> log = [];

        int Prepare(int item)
        {
            lock (log)
            {
                log.Add("prepare");
            }

            return item;
        }

        foreach ((int _, int prepared) in ParallelBatch.Prepare([9, 1, 1], parallelOptions, item => item, 2, Prepare))
        {
            log.Add($"consume {prepared}");
        }

        Assert.That(log, Is.EqualTo([
            "prepare", "consume 9",
            "prepare", "prepare", "consume 1", "consume 1"
        ]));
    }

    [Test]
    public void ZeroCostTest()
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        List<string> log = [];

        int Prepare(int item)
        {
            lock (log)
            {
                log.Add("prepare");
            }

            return item;
        }

        foreach ((int _, int prepared) in ParallelBatch.Prepare([0, 5, 0, 5, 0], parallelOptions, item => item, 5, Prepare))
        {
            log.Add($"consume {prepared}");
        }

        Assert.That(log, Is.EqualTo([
            "prepare", "prepare", "prepare", "consume 0", "consume 5", "consume 0",
            "prepare", "prepare", "consume 5", "consume 0"
        ]));
    }

    [Test]
    public void PreparationFailureTest()
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        List<(int Item, int Prepared)> results = [];

        int Prepare(int item)
        {
            throw new InvalidOperationException("Enhancement produced no output.");
        }

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => results.AddRange(ParallelBatch.Prepare([0, 1, 2], parallelOptions, _ => 1, 3, Prepare)));

        Assert.That(ex.Message, Is.EqualTo("Enhancement produced no output."));
    }
}
