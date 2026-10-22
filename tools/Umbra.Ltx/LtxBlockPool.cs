using TorchSharp;

namespace Umbra.Ltx;

internal sealed class LtxBlockPool : IDisposable
{
    private readonly Queue<torch.Tensor> free = [];
    private readonly List<torch.Tensor> buffers = [];

    public LtxBlockPool(long bufferElements, int capacity, torch.ScalarType type, torch.Device device)
    {
        for (int index = 0; index < capacity; index++)
        {
            torch.Tensor buffer = torch.empty(bufferElements, type, device);

            buffer.DetachFromDisposeScope();
            buffers.Add(buffer);
            free.Enqueue(buffer);
        }

        Capacity = capacity;
    }

    public int Capacity { get; }

    public torch.Tensor Acquire()
    {
        if (free.Count <= 0)
        {
            throw new InvalidOperationException("Cannot acquire a free block buffer.");
        }

        return free.Dequeue();
    }

    public void Release(torch.Tensor buffer)
    {
        free.Enqueue(buffer);
    }

    public void Dispose()
    {
        foreach (torch.Tensor buffer in buffers)
        {
            buffer.Dispose();
        }

        buffers.Clear();
        free.Clear();
    }
}
