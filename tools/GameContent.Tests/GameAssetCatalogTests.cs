using Gallean.Engine.Contracts;

namespace GameContent.Tests;

public sealed class GameAssetCatalogTests
{
    private static string FormatKind(GameAssetDescriptor descriptor)
    {
        return descriptor switch
        {
            GameAssetDescriptor.Sprite => "sprite",
            GameAssetDescriptor.Animation => "animation",
            GameAssetDescriptor.Sound => "sound",
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor, null)
        };
    }

    private static IReadOnlyList<string> CountByCategory(IReadOnlyList<GameAssetDescriptor> descriptors)
    {
        SortedDictionary<string, int> byCategory = new(StringComparer.Ordinal);

        foreach (GameAssetDescriptor descriptor in descriptors)
        {
            string category = descriptor.Key.CanonicalId.Split('/')[0];
            string requirement = descriptor.Required ? "required" : "optional";
            string group = $"{FormatKind(descriptor)} {category} {requirement}";

            byCategory.TryGetValue(group, out int count);
            byCategory[group] = count + 1;
        }

        List<string> rows = [];

        foreach ((string group, int count) in byCategory)
        {
            rows.Add($"{group} {count}");
        }

        return rows;
    }

    private static bool Roundtrips(GameAssetDescriptor descriptor)
    {
        string id = descriptor.Key.CanonicalId;

        switch (descriptor)
        {
            case GameAssetDescriptor.Sprite:
                AssetSpriteKey.TryParse(id, null, out AssetSpriteKey? sprite);

                return descriptor.Key.Equals(sprite);
            case GameAssetDescriptor.Animation:
                AssetAnimationKey.TryParse(id, null, out AssetAnimationKey? animation);

                return descriptor.Key.Equals(animation);
            case GameAssetDescriptor.Sound:
                AssetSoundKey.TryParse(id, null, out AssetSoundKey? sound);

                return descriptor.Key.Equals(sound);
            default:
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor, null);
        }
    }

    private static IReadOnlyList<string> FindUnparsedKeys(IReadOnlyList<GameAssetDescriptor> descriptors)
    {
        List<string> unparsed = [];

        foreach (GameAssetDescriptor descriptor in descriptors)
        {
            if (!Roundtrips(descriptor))
            {
                unparsed.Add(descriptor.Key.CanonicalId);
            }
        }

        return unparsed;
    }

    [Test]
    public void ListDescriptorsTest()
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(
            new GameAssetInputs([new UnitDefinitionId("G000UU0001")], [], [], [], [], [], []));

        Assert.That(CountByCategory(descriptors), Is.EqualTo([
            "animation battle-effect-attack optional 54",
            "animation battle-effect-blister required 1",
            "animation battle-effect-death optional 8",
            "animation battle-effect-death required 8",
            "animation battle-effect-heal required 1",
            "animation battle-effect-item required 3",
            "animation battle-effect-level-up required 12",
            "animation battle-effect-status required 22",
            "animation battle-unit required 6",
            "animation battle-unit-aura optional 6",
            "animation battle-unit-shadow required 6",
            "animation capital required 12",
            "animation capital-background-overlay optional 10",
            "animation city required 5",
            "animation city-shadow optional 5",
            "animation cloud required 3",
            "animation crystal required 6",
            "animation cursor required 36",
            "animation main-menu-background-overlay required 2",
            "animation mountain-remove-effect required 3",
            "animation race-selection-background-overlay optional 10",
            "animation ruin required 22",
            "animation site required 21",
            "animation site-banner required 4",
            "animation site-shadow optional 21",
            "animation stack-battle-effect required 1",
            "animation stack-boat required 96",
            "animation stack-boat-shadow optional 96",
            "animation stack-create-effect required 1",
            "animation stack-destroy-effect required 1",
            "animation stack-owner-change-effect required 1",
            "animation stack-summon-destroy-effect required 1",
            "animation stack-unit optional 16",
            "animation stack-unit-aura optional 16",
            "animation stack-unit-shadow optional 16",
            "animation water required 1",
            "sound ambient optional 12",
            "sound ambient-capital optional 6",
            "sound ambient-music optional 11",
            "sound ambient-ruin optional 4",
            "sound bag-pick-up optional 1",
            "sound battle-effect-sound-death optional 1",
            "sound battle-effect-sound-heal optional 1",
            "sound battle-effect-sound-potion optional 3",
            "sound battle-effect-sound-status optional 6",
            "sound battle-music optional 4",
            "sound battle-unit-attack optional 6",
            "sound battle-unit-damaged optional 4",
            "sound battle-unit-hit optional 5",
            "sound battle-unit-touch optional 5",
            "sound beep optional 1",
            "sound book-close optional 1",
            "sound book-open optional 1",
            "sound briefing-music optional 1",
            "sound building-construct optional 1",
            "sound capital-music optional 18",
            "sound city-enter optional 1",
            "sound city-exit optional 1",
            "sound city-grow optional 1",
            "sound city-riot-end optional 1",
            "sound interface-open optional 1",
            "sound item-use optional 1",
            "sound menu-music optional 1",
            "sound occupy optional 1",
            "sound ruin-enter optional 1",
            "sound ruin-loot optional 1",
            "sound site-enter optional 1",
            "sound site-purchase optional 1",
            "sound spell-research optional 1",
            "sound stack-boat-move optional 1",
            "sound stack-create optional 1",
            "sound stack-destroy optional 1",
            "sound stack-summon-destroy optional 1",
            "sound stack-unit-attack optional 1",
            "sound stack-unit-death optional 1",
            "sound stack-unit-idle optional 2",
            "sound stack-unit-move optional 4",
            "sound stack-unit-response optional 4",
            "sound thief-action optional 1",
            "sound turn-start optional 1",
            "sound unit-purchase optional 1",
            "sound world-battle-start optional 1",
            "sound world-music optional 11",
            "sprite bag required 12",
            "sprite battle-corpse required 4",
            "sprite capital-background optional 10",
            "sprite capital-background required 5",
            "sprite capital-image required 5",
            "sprite city-image required 5",
            "sprite city-shield required 11",
            "sprite ground required 7",
            "sprite ground-border required 82",
            "sprite item-icon required 5",
            "sprite lord-face optional 42",
            "sprite main-menu-background required 1",
            "sprite mountain required 228",
            "sprite race-selection-background required 5",
            "sprite road required 16",
            "sprite rod required 5",
            "sprite ruin-banner required 2",
            "sprite site-image required 4",
            "sprite stack-banner required 15",
            "sprite thief-action-image required 28",
            "sprite tombstone required 2",
            "sprite tree required 139",
            "sprite unit-event-portrait optional 1",
            "sprite unit-face required 1",
            "sprite unit-portrait required 1"
        ]));
    }

    [Test]
    public void ListDescriptorsWithInputsTest()
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(new GameAssetInputs(
            [new UnitDefinitionId("G000UU0001")],
            [],
            [new WorldSpellDefinitionId("G000SS0001"), new WorldSpellDefinitionId("G000SS0002")],
            [new WorldPlayerBuildingDefinitionId("G000BB0001")],
            [new WorldLandmarkDefinitionId("G000MG0004")],
            [new ItemDefinitionId("G000IG0001")],
            [new ScenarioId("S135SC0000"), new ScenarioId("S253SC0000")]));

        Assert.That(CountByCategory(descriptors), Is.EqualTo([
            "animation battle-effect-attack optional 54",
            "animation battle-effect-blister required 1",
            "animation battle-effect-death optional 8",
            "animation battle-effect-death required 8",
            "animation battle-effect-heal required 1",
            "animation battle-effect-item required 3",
            "animation battle-effect-level-up required 12",
            "animation battle-effect-status required 22",
            "animation battle-unit required 6",
            "animation battle-unit-aura optional 6",
            "animation battle-unit-shadow required 6",
            "animation capital required 12",
            "animation capital-background-overlay optional 10",
            "animation capital-building required 1",
            "animation city required 5",
            "animation city-shadow optional 5",
            "animation cloud required 3",
            "animation crystal required 6",
            "animation cursor required 36",
            "animation landmark required 1",
            "animation main-menu-background-overlay required 2",
            "animation mountain-remove-effect required 3",
            "animation race-selection-background-overlay optional 10",
            "animation ruin required 22",
            "animation site required 21",
            "animation site-banner required 4",
            "animation site-shadow optional 21",
            "animation stack-battle-effect required 1",
            "animation stack-boat required 96",
            "animation stack-boat-shadow optional 96",
            "animation stack-create-effect required 1",
            "animation stack-destroy-effect required 1",
            "animation stack-owner-change-effect required 1",
            "animation stack-summon-destroy-effect required 1",
            "animation stack-unit optional 16",
            "animation stack-unit-aura optional 16",
            "animation stack-unit-shadow optional 16",
            "animation water required 1",
            "animation water-landmark required 1",
            "animation world-spell-effect optional 20",
            "sound ambient optional 12",
            "sound ambient-capital optional 6",
            "sound ambient-music optional 11",
            "sound ambient-ruin optional 4",
            "sound bag-pick-up optional 1",
            "sound battle-effect-sound-death optional 1",
            "sound battle-effect-sound-heal optional 1",
            "sound battle-effect-sound-potion optional 3",
            "sound battle-effect-sound-status optional 6",
            "sound battle-music optional 4",
            "sound battle-unit-attack optional 6",
            "sound battle-unit-damaged optional 4",
            "sound battle-unit-hit optional 5",
            "sound battle-unit-touch optional 5",
            "sound beep optional 1",
            "sound book-close optional 1",
            "sound book-open optional 1",
            "sound briefing-music optional 1",
            "sound briefing-voice optional 2",
            "sound building-construct optional 1",
            "sound capital-music optional 18",
            "sound city-enter optional 1",
            "sound city-exit optional 1",
            "sound city-grow optional 1",
            "sound city-riot-end optional 1",
            "sound interface-open optional 1",
            "sound item-use optional 1",
            "sound menu-music optional 1",
            "sound occupy optional 1",
            "sound ruin-enter optional 1",
            "sound ruin-loot optional 1",
            "sound site-enter optional 1",
            "sound site-purchase optional 1",
            "sound spell-research optional 1",
            "sound stack-boat-move optional 1",
            "sound stack-create optional 1",
            "sound stack-destroy optional 1",
            "sound stack-summon-destroy optional 1",
            "sound stack-unit-attack optional 1",
            "sound stack-unit-death optional 1",
            "sound stack-unit-idle optional 2",
            "sound stack-unit-move optional 4",
            "sound stack-unit-response optional 4",
            "sound thief-action optional 1",
            "sound turn-start optional 1",
            "sound unit-purchase optional 1",
            "sound victory-music optional 2",
            "sound world-battle-start optional 1",
            "sound world-music optional 11",
            "sound world-spell-cast optional 2",
            "sprite bag required 12",
            "sprite battle-corpse required 4",
            "sprite building-icon required 1",
            "sprite capital-background optional 10",
            "sprite capital-background required 5",
            "sprite capital-image required 5",
            "sprite city-image required 5",
            "sprite city-shield required 11",
            "sprite ground required 7",
            "sprite ground-border required 82",
            "sprite item-icon optional 1",
            "sprite item-icon required 5",
            "sprite lord-face optional 42",
            "sprite main-menu-background required 1",
            "sprite mountain required 228",
            "sprite race-selection-background required 5",
            "sprite road required 16",
            "sprite rod required 5",
            "sprite ruin-banner required 2",
            "sprite site-image required 4",
            "sprite spell-icon required 2",
            "sprite stack-banner required 15",
            "sprite thief-action-image required 28",
            "sprite tombstone required 2",
            "sprite tree required 139",
            "sprite unit-event-portrait optional 1",
            "sprite unit-face required 1",
            "sprite unit-portrait required 1"
        ]));
    }

    [Test]
    public void UniqueIdsTest()
    {
        UnitDefinitionId firstUnitId = new("G000UU0001");
        UnitDefinitionId secondUnitId = new("G000UU0002");
        ScenarioEventPortraitId namedPortraitId = new("FYRWENN");

        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(new GameAssetInputs(
            [firstUnitId, secondUnitId],
            [ScenarioEventPortraitId.Unit(firstUnitId), namedPortraitId],
            [],
            [],
            [],
            [],
            []));

        HashSet<string> ids = [];
        List<string> duplicates = [];
        List<AssetSpriteKey.UnitEventPortrait> eventPortraits = [];

        foreach (GameAssetDescriptor descriptor in descriptors)
        {
            string id = descriptor.Key.CanonicalId;

            if (!ids.Add(id))
            {
                duplicates.Add(id);
            }

            if (descriptor.Key is AssetSpriteKey.UnitEventPortrait eventPortrait)
            {
                eventPortraits.Add(eventPortrait);
            }
        }

        Assert.That(duplicates, Is.Empty);
        Assert.That(eventPortraits, Is.EqualTo([
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(firstUnitId)),
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(secondUnitId)),
            new AssetSpriteKey.UnitEventPortrait(namedPortraitId)
        ]));
    }

    [Test]
    public void ParseKeysTest()
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(
            new GameAssetInputs(
                [new UnitDefinitionId("G000UU0001")],
                [new ScenarioEventPortraitId("CENTAUR")],
                [new WorldSpellDefinitionId("G000SS0001")],
                [new WorldPlayerBuildingDefinitionId("G000BB0001")],
                [new WorldLandmarkDefinitionId("G000MG0004")],
                [new ItemDefinitionId("G000IG0001")],
                []));

        Assert.That(FindUnparsedKeys(descriptors), Is.Empty);
    }
}
