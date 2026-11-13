using System.Diagnostics;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class WorldScreenSnapshotWorker(
    IAssetProvider assets,
    IGameDefinitionProvider definitions,
    IGameScenarioProvider scenario,
    DiagnosticsRecorder diagnostics) : IDisposable
{
    private readonly AssetPreloader assetPreloader = new(assets);
    private readonly WorldRenderComposer composer = new(definitions, scenario, assets);
    private readonly Lock locker = new();
    private readonly WorldRenderContentProvider worldContentProvider = new();
    private volatile WorldScreenSnapshot? current;
    private bool disposed;
    private volatile Exception? exception;
    private WorldRenderRequest? latestRequest;
    private bool running;
    private Task workerTask = Task.CompletedTask;

    public WorldScreenSnapshot? Current => current;
    public Exception? Exception => exception;

    public void Dispose()
    {
        Task activeTask;

        lock (locker)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            activeTask = workerTask;
        }

        activeTask.Wait();
    }

    public void Request(WorldRenderRequest request)
    {
        lock (locker)
        {
            if (disposed || exception is not null)
            {
                return;
            }

            if (ReferenceEquals(latestRequest, request))
            {
                return;
            }

            latestRequest = request;

            if (!running)
            {
                running = true;
                workerTask = Task.Run(RunAsync);
            }
        }
    }

    private async Task RunAsync()
    {
        WorldRenderRequest? built = null;

        while (true)
        {
            WorldRenderRequest? input;

            lock (locker)
            {
                input = ReferenceEquals(latestRequest, built) ? null : latestRequest;

                if (input is null || disposed)
                {
                    running = false;
                    return;
                }
            }

            WorldScreenSnapshot snapshot;
            try
            {
                snapshot = await GetSnapshotAsync(input);
            }
            catch (Exception ex)
            {
                lock (locker)
                {
                    exception = ex;
                    running = false;
                }

                return;
            }

            current = snapshot;
            built = input;
        }
    }

    private async Task<WorldScreenSnapshot> GetSnapshotAsync(WorldRenderRequest request)
    {
        long startTimestamp = Stopwatch.GetTimestamp();
        WorldRenderState state = composer.Compose(request);
        WorldScreenSnapshot? snapshot = current;
        WorldScreenSnapshot result;

        if (snapshot is null || !ReferenceEquals(snapshot.WorldState, state))
        {
            result = await BuildAsync(request, state, snapshot, startTimestamp);
        }
        else
        {
            result = snapshot with { RenderRequest = request };
        }

        return result;
    }

    private async Task<WorldScreenSnapshot> BuildAsync(
        WorldRenderRequest request,
        WorldRenderState state,
        WorldScreenSnapshot? previous,
        long startTimestamp)
    {
        bool measure = diagnostics.IsEnabled;

        if (measure && previous is not null && !state.Cursors.SequenceEqual(previous.WorldState.Cursors))
        {
            diagnostics.AddHoverChange();
        }

        await PreloadAssetsAsync(state);

        WorldRenderContent worldContent = worldContentProvider.Get(state, assets);

        if (measure)
        {
            diagnostics.AddBuild(Stopwatch.GetElapsedTime(startTimestamp));
        }

        return new WorldScreenSnapshot(request, worldContent, state);
    }

    private async Task PreloadAssetsAsync(WorldRenderState state)
    {
        if (state.Animation is not null)
        {
            AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(state.Animation);

            using IDisposable leases = await assetPreloader.PreloadAsync([.. plan.InitialRequired, .. plan.InitialOptional]);
        }
    }
}
