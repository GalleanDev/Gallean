namespace Packer;

internal static class RefineConfigurationMap
{
    private static readonly RefineConfiguration BattleUnit = new(MinClipDimension: 90);
    private static readonly RefineConfiguration Default = new(MinClipDimension: 128);

    public static RefineConfiguration GetConfiguration(AssetKey key)
    {
        if (key is AssetAnimationKey.BattleUnit)
        {
            return BattleUnit;
        }
        else
        {
            return Default;
        }
    }
}
