namespace Umbra;

public static class UmbraFluidBuilder
{
    public static UmbraFluidLoop Build(UmbraFluidConfiguration configuration, UmbraFluidMap map, int width, int height)
    {
        int simulationWidth = Math.Max(1, (int)Math.Round(width * configuration.SimulationScale));
        int simulationHeight = Math.Max(1, (int)Math.Round(height * configuration.SimulationScale));

        FluidControl fluidControl = FluidControl.Create(configuration, map, simulationWidth, simulationHeight);
        FluidSolver fluidSolver = new(configuration, fluidControl, width, height);

        return fluidSolver.Solve();
    }
}
