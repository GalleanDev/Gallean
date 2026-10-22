using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class MosrDySample : Module<torch.Tensor, torch.Tensor>
{
    private const double OffsetScale = 0.5;

    private readonly Conv2d offset;
    private readonly Conv2d scope;
    [ComponentName(Name = "end_conv")]
    private readonly Conv2d endConv;
    private readonly int scale;
    private readonly int groups;

    public MosrDySample(int inChannels, int outChannels, int scale, int groups)
        : base(nameof(MosrDySample))
    {
        this.scale = scale;
        this.groups = groups;

        int channels = 2 * groups * scale * scale;

        offset = Conv2d(inChannels, channels, kernel_size: 1, stride: 1, padding: 0L, bias: true);
        scope = Conv2d(inChannels, channels, kernel_size: 1, stride: 1, padding: 0L, bias: false);
        endConv = Conv2d(inChannels, outChannels, kernel_size: 1, stride: 1, padding: 0L, bias: true);

        register_buffer("init_pos", BuildInitialPositions(scale, groups));

        RegisterComponents();
    }

    public static torch.Tensor BuildInitialPositions(int scale, int groups)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor steps = torch.arange((-scale + 1) / 2.0, ((scale - 1) / 2.0) + 1, 1.0) / scale;
        using torch.Tensor rows = steps.unsqueeze(1).expand(scale, scale);
        using torch.Tensor columns = steps.unsqueeze(0).expand(scale, scale);
        using torch.Tensor stacked = torch.stack([rows, columns], dim: 0);
        using torch.Tensor transposed = stacked.transpose(1, 2);
        using torch.Tensor repeated = transposed.reshape(2, 1, scale * scale).repeat(1, groups, 1);

        torch.Tensor result = repeated.reshape(1, -1, 1, 1);

        return result.MoveToOuterDisposeScope();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable disposeScope = torch.NewDisposeScope();

        torch.Tensor buffer = get_buffer("init_pos") ?? throw new InvalidOperationException("The sampler has no initial positions.");
        using torch.Tensor initialPositions = buffer.to(input.dtype, input.device);

        using torch.Tensor gate = scope.call(input).sigmoid();
        using torch.Tensor scaled = offset.call(input) * gate * OffsetScale;
        using torch.Tensor offsets = scaled + initialPositions;

        long batch = offsets.shape[0];
        long height = offsets.shape[2];
        long width = offsets.shape[3];

        using torch.Tensor grouped = offsets.view(batch, 2, -1, height, width);
        using torch.Tensor coordinates = BuildCoordinates(height, width, input.dtype, input.device);
        using torch.Tensor placed = coordinates + grouped;
        using torch.Tensor normalizer = torch.tensor(
            new float[] { width, height }, input.dtype, input.device).view(1, 2, 1, 1, 1);

        using torch.Tensor normalized = (2.0 * placed / normalizer) - 1.0;
        using torch.Tensor flattened = normalized.reshape(batch, -1, height, width);
        using torch.Tensor shuffled = functional.pixel_shuffle(flattened, scale);
        using torch.Tensor split = shuffled.view(batch, 2, -1, scale * height, scale * width);
        using torch.Tensor ordered = split.permute(0, 2, 3, 4, 1).contiguous().flatten(0, 1);
        using torch.Tensor samples = input.reshape(batch * groups, -1, height, width);

        using torch.Tensor sampled = functional.grid_sample(
            samples,
            ordered,
            torch.GridSampleMode.Bilinear,
            torch.GridSamplePaddingMode.Border,
            align_corners: false);

        using torch.Tensor merged = sampled.view(batch, -1, scale * height, scale * width);

        torch.Tensor result = endConv.call(merged);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor BuildCoordinates(long height, long width, torch.ScalarType dtype, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor columns = torch.arange(width, dtype, device) + 0.5;
        using torch.Tensor rows = torch.arange(height, dtype, device) + 0.5;
        using torch.Tensor columnGrid = columns.unsqueeze(1).expand(width, height);
        using torch.Tensor rowGrid = rows.unsqueeze(0).expand(width, height);
        using torch.Tensor stacked = torch.stack([columnGrid, rowGrid], dim: 0);
        using torch.Tensor transposed = stacked.transpose(1, 2);

        torch.Tensor result = transposed.unsqueeze(1).unsqueeze(0);

        return result.MoveToOuterDisposeScope();
    }
}
