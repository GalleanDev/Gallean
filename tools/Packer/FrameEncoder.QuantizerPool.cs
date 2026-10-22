using System.Collections.Concurrent;

namespace Packer;

internal static partial class FrameEncoder
{
    public sealed class QuantizerPool
    {
        private readonly ConcurrentBag<WuQuantizer> quantizers = [];

        public WuQuantizer Rent()
        {
            if (quantizers.TryTake(out WuQuantizer? quantizer))
            {
                quantizer.Reset();

                return quantizer;
            }
            else
            {
                return new WuQuantizer(PaletteColors);
            }
        }

        public void Return(WuQuantizer quantizer)
        {
            quantizers.Add(quantizer);
        }
    }
}
