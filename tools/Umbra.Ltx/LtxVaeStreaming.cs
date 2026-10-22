using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxVaeStreaming
{
    public static torch.Tensor Apply(
        ILtxVaeStage stage,
        torch.Tensor input,
        torch.Device device,
        long budgetBytes,
        long residentBytes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long outputFrames = stage.GetOutputFrames(input.shape[2]);
        long chunkFrames = GetChunkFrames(stage, input, outputFrames, budgetBytes);
        torch.Tensor? output = null;

        for (long start = 0; start < outputFrames; start += chunkFrames)
        {
            long count = Math.Min(chunkFrames, outputFrames - start);

            using IDisposable chunkScope = torch.NewDisposeScope();

            using torch.Tensor chunk = stage.ForwardFrames(input, start, count, device);

            if (output is null)
            {
                long outputBytes = chunk.NumberOfElements / count * outputFrames * chunk.element_size();
                torch.Device storage = outputBytes <= residentBytes ? device : torch.CPU;

                output = torch.empty(
                    [chunk.shape[0], chunk.shape[1], outputFrames, chunk.shape[3], chunk.shape[4]],
                    chunk.dtype,
                    storage).MoveToOuterDisposeScope();
            }

            using torch.Tensor target = output.narrow(2, start, count);

            target.copy_(chunk);
        }

        if (output is null)
        {
            throw new InvalidOperationException("The stage produced no frames.");
        }

        return output.MoveToOuterDisposeScope();
    }

    private static long GetChunkFrames(ILtxVaeStage stage, torch.Tensor input, long outputFrames, long budgetBytes)
    {
        long inputFrameBytes = input.shape[0] * input.shape[1] * input.shape[3] * input.shape[4] * input.element_size();
        long chunkFrames = 1;

        while (chunkFrames < outputFrames && stage.GetPeakBytes(chunkFrames + 1, inputFrameBytes) <= budgetBytes)
        {
            chunkFrames++;
        }

        return chunkFrames;
    }
}
