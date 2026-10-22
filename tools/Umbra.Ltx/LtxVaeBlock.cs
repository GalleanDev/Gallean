namespace Umbra.Ltx;

internal sealed record LtxVaeBlock(LtxVaeBlockType Type, int Count)
{
    public (int Time, int Height, int Width) Stride => Type switch
    {
        LtxVaeBlockType.Residual => (1, 1, 1),
        LtxVaeBlockType.CompressSpace => (1, 2, 2),
        LtxVaeBlockType.CompressTime => (2, 1, 1),
        LtxVaeBlockType.CompressAll => (2, 2, 2)
    };

    public int StrideProduct
    {
        get
        {
            (int time, int height, int width) = Stride;

            return time * height * width;
        }
    }
}
