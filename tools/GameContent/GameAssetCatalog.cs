namespace GameContent;

public static class GameAssetCatalog
{
    public static IReadOnlyList<GameAssetDescriptor> ListDescriptors(GameAssetInputs inputs)
    {
        List<GameAssetDescriptor> descriptors =
        [
            .. GameInterfaceSource.ListDescriptors(),
            .. GameCursorSource.ListDescriptors(),
            .. GameTerrainSource.ListDescriptors(),
            .. GameWorldObjectSource.ListDescriptors(inputs.LandmarkIds),
            .. GameCapitalSource.ListDescriptors(inputs.BuildingIds),
            .. GameCitySource.ListDescriptors(),
            .. GameBattleSource.ListDescriptors(inputs.UnitIds),
            .. GameStackSource.ListDescriptors(inputs.UnitIds),
            .. GamePortraitSource.ListDescriptors(inputs.UnitIds, inputs.EventPortraitIds),
            .. GameIconSource.ListDescriptors(inputs.SpellIds, inputs.BuildingIds, inputs.ItemIds),
            .. GameSpellEffectSource.ListDescriptors(inputs.SpellIds),
            .. GameAudioSource.ListDescriptors(inputs.UnitIds, inputs.SpellIds, inputs.ScenarioIds)
        ];

        return descriptors;
    }
}
