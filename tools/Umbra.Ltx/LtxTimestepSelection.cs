using TorchSharp;

namespace Umbra.Ltx;

internal sealed record LtxTimestepSelection(torch.Tensor Values, torch.Tensor Index)
{
    private readonly Lazy<IReadOnlyList<LtxTimestepRun>> runs = new(() => ComputeRuns(Index));

    public IReadOnlyList<LtxTimestepRun> Runs => runs.Value;

    public static LtxTimestepSelection FromTokens(torch.Tensor timesteps)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor flatTimesteps = timesteps.flatten();

        (torch.Tensor values, torch.Tensor? _, torch.Tensor? _) = torch.unique(flatTimesteps);

        using torch.Tensor ownedValues = values;
        using torch.Tensor positions = torch.searchsorted(ownedValues, flatTimesteps);

        torch.Tensor index = positions.view(timesteps.shape[0], timesteps.shape[1]);

        return new LtxTimestepSelection(
            ownedValues.contiguous().MoveToOuterDisposeScope(),
            index.contiguous().MoveToOuterDisposeScope());
    }

    public torch.Tensor Gather(torch.Tensor table)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor flatIndex = Index.flatten();
        using torch.Tensor rows = table.index_select(0, flatIndex);

        long[] shape = new long[table.shape.Length + 1];

        shape[0] = Index.shape[0];
        shape[1] = Index.shape[1];

        for (int dimension = 1; dimension < table.shape.Length; dimension++)
        {
            shape[dimension + 1] = table.shape[dimension];
        }

        return rows.view(shape).MoveToOuterDisposeScope();
    }

    private static IReadOnlyList<LtxTimestepRun> ComputeRuns(torch.Tensor index)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor flatIndex = index.flatten().to(torch.ScalarType.Int64).cpu();

        long[] values = flatIndex.data<long>().ToArray();
        List<LtxTimestepRun> result = [];
        long start = 0;

        for (long position = 1; position <= values.Length; position++)
        {
            if (position == values.Length || values[position] != values[start])
            {
                result.Add(new LtxTimestepRun(start, position - start, values[start]));
                start = position;
            }
        }

        return result;
    }
}
