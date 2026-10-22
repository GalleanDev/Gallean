namespace Gallean.Client;

internal static class BattleEffectAttackExtensions
{
    extension(AssetAnimationKey.BattleEffect.Attack key)
    {
        public AssetAnimationKey.BattleEffect.Attack WithVariant(int variant)
        {
            return new AssetAnimationKey.BattleEffect.Attack(key.AttackerUnitId, key.Type, key.Layer, key.Side, variant);
        }
    }
}
