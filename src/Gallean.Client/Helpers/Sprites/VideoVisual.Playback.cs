using Avalonia;

namespace Gallean.Client;

internal sealed partial class VideoVisual
{
    private sealed class Playback(IAssetProvider assets, AssetAnimationKey key, AssetAnimationMetadata metadata)
    {
        public IAssetProvider Assets { get; } = assets;
        public AssetAnimationKey Key { get; } = key;
        public AssetAnimationMetadata Metadata { get; } = metadata;
        public TimeSpan? ClockStart { get; set; }
        public int FrameIndex { get; set; } = -1;
        public PixelSize? FrameSize { get; set; }
        public bool IsLoading { get; set; }
    }
}
