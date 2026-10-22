namespace Packer;

internal sealed class SetupEnvCommandHandler(PackerOutput output, EnvironmentBuilder builder)
{
    public async Task<int> RunAsync(SetupEnvCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        output.WriteLine($"Setting up the environment for the {commandOptions.Device}.");

        try
        {
            IReadOnlyList<EnvironmentComponent> components = await builder.BuildAsync(commandOptions.Device, cancellationToken);

            foreach (EnvironmentComponent component in components)
            {
                output.WriteLine($"{component.Name}: {component.Directory}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            output.WriteError($"Failed to set up the environment: {ex.Message}");

            return 1;
        }
    }
}
