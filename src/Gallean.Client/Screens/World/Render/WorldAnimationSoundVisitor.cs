using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed class WorldAnimationSoundVisitor : IWorldAnimationVisitor<Empty, AssetSoundKey?>, IStaticInstance<WorldAnimationSoundVisitor>
{
    public AssetSoundKey? Visit(StackMove animation, Empty context) => animation.SoundKey;
    public AssetSoundKey? Visit(Effect animation, Empty context) => animation.SoundKey;
    public AssetSoundKey? Visit(SpellCast animation, Empty context) => animation.SoundKey;
    public AssetSoundKey? Visit(Pause animation, Empty context) => null;
    public AssetSoundKey? Visit(StackBattle animation, Empty context) => null;
    public AssetSoundKey? Visit(BattleTransition animation, Empty context) => null;
}
