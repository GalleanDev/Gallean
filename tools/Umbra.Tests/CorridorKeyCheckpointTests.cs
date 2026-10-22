using Gallean.Configuration;
using TorchSharp;

namespace Umbra.Tests;

public sealed class CorridorKeyCheckpointTests
{
    private static string RequireModelPath()
    {
        string path = Path.Combine(AppDirectories.EnhanceModels, UmbraAlphaModel.DirectoryName, UmbraAlphaModel.BlueFileName);

        if (!File.Exists(path))
        {
            Assert.Ignore($"Unable to find model at '{path}'.");
        }

        return path;
    }

    [Test]
    [Explicit]
    public void ForwardTest()
    {
        torch.set_num_threads(Environment.ProcessorCount);

        string path = RequireModelPath();
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-checkpoint");
        CorridorKeyOptions options = CorridorKeyOptions.BasePlus(512);

        using CorridorKeyFormer former = MetaDevice.Construct(() => new CorridorKeyFormer(options));

        ModuleParameters.Load(former, AlphaWeights.Map(path, options, torch.ScalarType.Float32), torch.CPU);

        former.eval();

        using (torch.no_grad())
        {
            using torch.Tensor prepared = AlphaImage.Prepare(dump["colors"], dump["hint"], options.ImageSize);
            using CorridorKeyPrediction prediction = former.Forward(prepared);

            Assert.That(TestHelpers.MaxDifference(prediction.Alpha, dump["alpha"]), Is.LessThan(1e-3f));
            Assert.That(TestHelpers.MaxDifference(prediction.Foreground, dump["foreground"]), Is.LessThan(1e-3f));
        }
    }
}
