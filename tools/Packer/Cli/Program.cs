namespace Packer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        PackerOutput output = new(Console.Out, Console.Error);

        return await PackerCli.Create(output).Parse(args).InvokeAsync();
    }
}
