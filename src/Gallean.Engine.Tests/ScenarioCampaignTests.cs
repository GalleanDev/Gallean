using Gallean.Common;
using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class ScenarioCampaignTests
{
    private CampaignDatabase campaignDb = null!;
    private GlobalDatabase globalDb = null!;
    private ScenarioDatabase scenarioDb = null!;
    private IGameTextProvider texts = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        texts = new GameTextProvider(globalDb, scenarioDb, scenarioDb.TextScenarios);
    }

    private static IReadOnlyList<string> FormatCampaigns(IReadOnlyList<ScenarioCampaign> campaigns, IGameTextProvider texts)
    {
        List<string> rows = [];

        foreach (ScenarioCampaign campaign in campaigns)
        {
            string name = texts.GetGlobalText(campaign.NameTextId).Text;
            string raceName = texts.GetGlobalText(campaign.PlayRace.NameTextId).Text;

            rows.Add($"{campaign.Id} [{campaign.PlayRace.Race}/{raceName}] {name}");

            foreach (ScenarioMetadata scenario in campaign.Scenarios)
            {
                string scenarioName = texts.GetScenarioText(scenario.InfoTexts.Name);
                string creator = string.Empty;

                if (scenario.InfoTexts.Creator is { } creatorTextId)
                {
                    creator = texts.GetScenarioText(creatorTextId);
                }

                JoinStringBuilder races = new(", ");

                foreach (ScenarioRace race in scenario.PlayableRaces)
                {
                    races.Add($"{race.Race}/{texts.GetGlobalText(race.NameTextId).Text}");
                }

                rows.Add(
                    $"  {scenario.Id} [lvl {scenario.SuggestedLevel}, {scenario.MapSize}x{scenario.MapSize}, " +
                    $"{scenario.CityCount} cities, {scenario.Difficulty}, {creator}] [{races}] {scenarioName}");
            }
        }

        return rows;
    }

    [Test]
    public void CampaignCatalogTest()
    {
        IReadOnlyList<ScenarioCampaign> campaigns = ScenarioLoader.EnumerateCampaigns(globalDb, scenarioDb, campaignDb);

        Assert.That(FormatCampaigns(campaigns, texts), Is.EqualTo([
            "C000CC0001 [Neutral/Neutral] Skirmish",
            "  S200SC0000 [lvl 5, 48x48, 4 cities, Average, Emanuel Wall] [Undead/Undead Hordes, Elf/Elven Alliance] 5th Column",
            "  S201SC0000 [lvl 10, 120x120, 21 cities, Average, Emanuel Wall] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Elf/Elven Alliance] A Pact With The Devil",
            "  S202SC0000 [lvl 1, 48x48, 5 cities, Easy, E Protopapas] [Human/Empire, Heretic/Legions of the Damned] A Return To Simpler Times",
            "  S203SC0000 [lvl 1, 48x48, 7 cities, Average, Emanuel Wall] [Dwarf/Mountain Clans, Elf/Elven Alliance] Alliance-Mountain War",
            "  S204SC0000 [lvl 1, 72x72, 8 cities, Average, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Elf/Elven Alliance] Arrival of the Pilgrims",
            "  S205SC0000 [lvl 1, 72x72, 10 cities, Easy, Emanuel Wall] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] Barbarians Rage",
            "  S206SC0000 [lvl 1, 48x48, 4 cities, Easy, Ariel Gauthier] [Human/Empire, Undead/Undead Hordes] Battle of the Yldram Isles",
            "  S207SC0000 [lvl 1, 72x72, 7 cities, Easy, Emanuel Protopapas] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] City Of The Lost Tribe",
            "  S208SC0000 [lvl 1, 48x48, 5 cities, Easy, Emanuel Wall] [Human/Empire, Undead/Undead Hordes] Claimed By The Sea",
            "  S209SC0000 [lvl 1, 72x72, 9 cities, Easy, Ethan Petty] [Heretic/Legions of the Damned, Dwarf/Mountain Clans] Cornered Beast",
            "  S210SC0000 [lvl 1, 72x72, 9 cities, Average, Eva Bunodiere] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] Corpus Delicti",
            "  S211SC0000 [lvl 1, 48x48, 4 cities, Easy, Eric Tougas] [Human/Empire, Heretic/Legions of the Damned] Crossing the Lucian",
            "  S212SC0000 [lvl 1, 48x48, 4 cities, Easy, Emanuel Wall] [Human/Empire, Heretic/Legions of the Damned] Death 'N' Dragons",
            "  S213SC0000 [lvl 1, 48x48, 5 cities, Easy, Emanuel Protopapas] [Heretic/Legions of the Damned, Dwarf/Mountain Clans] Devil's Night",
            "  S214SC0000 [lvl 1, 48x48, 4 cities, Average, Emanuel Wall] [Dwarf/Mountain Clans, Elf/Elven Alliance] Doragon's Realm",
            "  S215SC0000 [lvl 1, 48x48, 4 cities, Easy, Eva Bunodiere] [Human/Empire, Undead/Undead Hordes] Drega Zul",
            "  S216SC0000 [lvl 1, 144x144, 24 cities, Average, Emanuel Wall] [Human/Empire, Elf/Elven Alliance] Empire-Alliance War",
            "  S217SC0000 [lvl 1, 48x48, 4 cities, Average, Strategy First] [Elf/Elven Alliance, Human/Empire] Fardale's Web",
            "  S218SC0000 [lvl 1, 72x72, 6 cities, Average, Ethan Petty] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Elf/Elven Alliance] Flight",
            "  S219SC0000 [lvl 5, 96x96, 12 cities, Easy, Richard Therrien] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Greenskins",
            "  S220SC0000 [lvl 2, 72x72, 11 cities, Easy, Emanuel Protopapas] [Heretic/Legions of the Damned, Undead/Undead Hordes, Human/Empire] Hail Stone",
            "  S221SC0000 [lvl 1, 48x48, 3 cities, Average, Emanuel Wall] [Undead/Undead Hordes, Elf/Elven Alliance] Harvesting The Dead",
            "  S222SC0000 [lvl 1, 48x48, 5 cities, Easy, Emanuel Protopapas] [Human/Empire, Undead/Undead Hordes] Hatred",
            "  S223SC0000 [lvl 1, 48x48, 4 cities, Average, Strategy First] [Undead/Undead Hordes, Elf/Elven Alliance] Head of the Beast",
            "  S224SC0000 [lvl 1, 48x48, 5 cities, Easy, Eva Bunodiere] [Human/Empire, Dwarf/Mountain Clans] Hellish Invasion",
            "  S225SC0000 [lvl 1, 48x48, 2 cities, Easy, Eva Bunodiere] [Dwarf/Mountain Clans, Human/Empire] Hureth Valley",
            "  S226SC0000 [lvl 1, 72x72, 7 cities, Easy, Emanuel Protopapas] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes] Ice-Mar",
            "  S227SC0000 [lvl 1, 48x48, 4 cities, Easy, Emanuel Protopapas] [Dwarf/Mountain Clans, Heretic/Legions of the Damned] In Gold We Trust",
            "  S228SC0000 [lvl 1, 72x72, 6 cities, Easy, Ariel Gauthier] [Human/Empire, Dwarf/Mountain Clans] Kadrath's Fall",
            "  S229SC0000 [lvl 1, 48x48, 2 cities, Easy, Eva Bunodiere] [Dwarf/Mountain Clans, Undead/Undead Hordes] Last Stand",
            "  S230SC0000 [lvl 12, 96x96, 13 cities, Average, Emanuel] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] Legends Of The Grave",
            "  S231SC0000 [lvl 1, 72x72, 9 cities, Easy, Eva Bunodiere] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes] Lunnark's Travels",
            "  S232SC0000 [lvl 3, 48x48, 3 cities, Average, Julien Fullum] [Human/Empire, Elf/Elven Alliance] Mercantile",
            "  S233SC0000 [lvl 1, 96x96, 12 cities, Average, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] Nyullokth's Vengeance",
            "  S234SC0000 [lvl 1, 72x72, 8 cities, Easy, Emanuel Protopapas] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Orc Champions",
            "  S235SC0000 [lvl 1, 72x72, 8 cities, Easy, Emanuel Protopapas] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Orc Champions Advanced",
            "  S236SC0000 [lvl 1, 96x96, 9 cities, Easy, Eva Bunodiere] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Power To Corrupt",
            "  S237SC0000 [lvl 10, 48x48, 4 cities, Average, Emanuel Wall] [Undead/Undead Hordes, Elf/Elven Alliance] Rage Of The Damned",
            "  S238SC0000 [lvl 1, 48x48, 4 cities, Easy, Emanuel Protopapas] [Dwarf/Mountain Clans, Undead/Undead Hordes] Rivers Of The Damned",
            "  S239SC0000 [lvl 1, 144x144, 15 cities, Average, Emanuel Wall] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] Sea Serpent Fleet",
            "  S240SC0000 [lvl 1, 72x72, 6 cities, Average, G. Bourbonniere] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes] Siege of Katras",
            "  S241SC0000 [lvl 1, 72x72, 10 cities, Easy, Emanuel Wall] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] Spider's Lair",
            "  S242SC0000 [lvl 1, 72x72, 10 cities, Easy, P Sotos- E Bunodiere] [Human/Empire, Dwarf/Mountain Clans] Terror in Moonsong",
            "  S243SC0000 [lvl 1, 72x72, 9 cities, Average, Erin Martel] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] The Anjajuar",
            "  S244SC0000 [lvl 1, 48x48, 5 cities, Easy, Emanuel Protopapas] [Human/Empire, Heretic/Legions of the Damned] The Celestial King",
            "  S245SC0000 [lvl 3, 72x72, 15 cities, Average, Sylvain Schmidt] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] The Changing Lands",
            "  S246SC0000 [lvl 1, 48x48, 5 cities, Average, Emanuel Wall] [Human/Empire, Elf/Elven Alliance] The Lazarus Well",
            "  S247SC0000 [lvl 1, 48x48, 4 cities, Average, Emanuel Wall] [Heretic/Legions of the Damned, Elf/Elven Alliance] The Lord's Death",
            "  S248SC0000 [lvl 1, 48x48, 10 cities, Average, Erin Martel] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] The Queen's Dilemma",
            "  S249SC0000 [lvl 10, 48x48, 1 cities, Average, Richard Therrien] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] The Seals",
            "  S250SC0000 [lvl 1, 72x72, 9 cities, Easy, Ludovic Pinard] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned] The Way to Freedom",
            "  S251SC0000 [lvl 1, 72x72, 6 cities, Average, Strategy First] [Human/Empire, Dwarf/Mountain Clans, Elf/Elven Alliance, Undead/Undead Hordes] Three Temples",
            "  S252SC0000 [lvl 12, 72x72, 10 cities, Average, Eva Bunodiere] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] Totem's Retribution",
            "  S253SC0000 [lvl 1, 48x48, 3 cities, Easy, Eva Bunodiere] [Human/Empire] Tutorial",
            "  S254SC0000 [lvl 1, 48x48, 4 cities, Easy, Eva Bunodiere] [Human/Empire, Heretic/Legions of the Damned] Vaglan 1 - The Highlands",
            "  S255SC0000 [lvl 1, 72x72, 10 cities, Easy, Eva Bunodiere] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes] Vaglan 2 - The Betrayal",
            "  S256SC0000 [lvl 1, 72x72, 8 cities, Easy, Emanuel Protopapas] [Heretic/Legions of the Damned, Undead/Undead Hordes] Vertigo",
            "  S257SC0000 [lvl 2, 96x96, 13 cities, Easy, Emanuel Protopapas] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Vyllborn Massacre",
            "  S258SC0000 [lvl 3, 48x48, 3 cities, Average, Emanuel Wall] [Human/Empire, Elf/Elven Alliance] Without Mercy",
            "  S259SC0000 [lvl 1, 48x48, 6 cities, Average, Emanuel Wall] [Dwarf/Mountain Clans, Elf/Elven Alliance] Wotan Retribution",
            "C000CC0002 [Human/Empire] Disciples II - Dark Prophecy campaign",
            "  S107SC0000 [lvl 1, 48x48, 5 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned] The Necromancer's Spirit",
            "  S108SC0000 [lvl 1, 72x72, 7 cities, Easy, Strategy First] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned] The Alliance",
            "  S095SC0000 [lvl 1, 48x48, 4 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned] Antagonists",
            "  S096SC0000 [lvl 1, 72x72, 8 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Dwarf/Mountain Clans] Uther's Crusade",
            "  S097SC0000 [lvl 1, 72x72, 8 cities, Easy, Strategy First] [Human/Empire, Undead/Undead Hordes, Heretic/Legions of the Damned] Slander and Barbarism",
            "  S098SC0000 [lvl 1, 72x72, 8 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Dwarf/Mountain Clans] The Celebration",
            "  S099SC0000 [lvl 1, 96x96, 13 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Dwarf/Mountain Clans, Undead/Undead Hordes] Binding Forces",
            "C000CC0003 [Dwarf/Mountain Clans] Disciples II - Dark Prophecy campaign",
            "  S116SC0000 [lvl 1, 48x48, 3 cities, Easy, Erin Martel] [Dwarf/Mountain Clans, Heretic/Legions of the Damned] Lost Runes",
            "  S117SC0000 [lvl 2, 48x48, 6 cities, Easy, Erin Martel] [Human/Empire, Dwarf/Mountain Clans] The Charmed Child",
            "  S118SC0000 [lvl 1, 72x72, 8 cities, Easy, Erin Martel] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] War Spirits",
            "  S119SC0000 [lvl 1, 48x48, 3 cities, Easy, Erin Martel] [Dwarf/Mountain Clans, Heretic/Legions of the Damned] New Leadership",
            "  S120SC0000 [lvl 1, 72x72, 10 cities, Easy, Erin Martel] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] The Unholy Siege",
            "  S121SC0000 [lvl 1, 72x72, 8 cities, Easy, Erin Martel] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] The Ceremony",
            "  S122SC0000 [lvl 1, 96x96, 13 cities, Easy, Erin Martel] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] The Days Before Ragnarok",
            "C000CC0004 [Heretic/Legions of the Damned] Disciples II - Dark Prophecy campaign",
            "  S100SC0000 [lvl 1, 48x48, 3 cities, Easy, Strategy First] [Heretic/Legions of the Damned, Dwarf/Mountain Clans] The Broken Seal",
            "  S101SC0000 [lvl 1, 72x72, 8 cities, Easy, Strategy First] [Undead/Undead Hordes, Heretic/Legions of the Damned] Bethrezen's Retreat",
            "  S102SC0000 [lvl 1, 72x72, 7 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Dwarf/Mountain Clans] The Dark Alliance",
            "  S103SC0000 [lvl 1, 72x72, 8 cities, Easy, Strategy First] [Heretic/Legions of the Damned, Dwarf/Mountain Clans] The Salty Dungeon",
            "  S104SC0000 [lvl 1, 72x72, 6 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned, Dwarf/Mountain Clans] Darkness and Light",
            "  S105SC0000 [lvl 1, 48x48, 2 cities, Easy, Strategy First] [Human/Empire, Heretic/Legions of the Damned] The Ambush",
            "  S106SC0000 [lvl 1, 96x96, 9 cities, Easy, Strategy First] [Human/Empire, Undead/Undead Hordes, Heretic/Legions of the Damned, Dwarf/Mountain Clans] The Prince of Hell",
            "C000CC0005 [Undead/Undead Hordes] Disciples II - Dark Prophecy campaign",
            "  S109SC0000 [lvl 1, 48x48, 3 cities, Easy, Stategy First] [Human/Empire, Undead/Undead Hordes] The Search for Timmoria",
            "  S110SC0000 [lvl 1, 72x72, 8 cities, Easy, Strategy First] [Undead/Undead Hordes, Heretic/Legions of the Damned] Uther's Blood",
            "  S111SC0000 [lvl 1, 72x72, 6 cities, Easy, Strategy First] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] The Council",
            "  S112SC0000 [lvl 1, 72x72, 7 cities, Easy, Strategy First] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes] Retaliation",
            "  S113SC0000 [lvl 1, 72x72, 7 cities, Easy, Strategy First] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] The Accursed Armor",
            "  S114SC0000 [lvl 1, 72x72, 6 cities, Easy, Strategy First] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Phantom Beasts",
            "  S115SC0000 [lvl 1, 96x96, 10 cities, Easy, Strategy First] [Human/Empire, Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes] Taladrielle the Sorceress",
            "C000CC0006 [Elf/Elven Alliance] Disciples II - Elven Alliance campaign",
            "  S135SC0000 [lvl 1, 48x48, 3 cities, Average, Ethan Petty] [Undead/Undead Hordes, Elf/Elven Alliance] The Sins of Our Fathers",
            "  S136SC0000 [lvl 1, 48x48, 4 cities, Average, Ethan Petty] [Heretic/Legions of the Damned, Elf/Elven Alliance] The Hunt",
            "  S137SC0000 [lvl 1, 48x48, 1 cities, Average, Julien Fullum] [Elf/Elven Alliance] Conquest of Thelena",
            "  S138SC0000 [lvl 1, 72x72, 10 cities, Average, Julien Fullum] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Elf/Elven Alliance] Korbach's Fort",
            "  S139SC0000 [lvl 1, 72x72, 8 cities, Average, Julien Fullum] [Dwarf/Mountain Clans, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] The Betrayal",
            "  S140SC0000 [lvl 1, 48x48, 3 cities, Average, Julien Fullum] [Human/Empire, Elf/Elven Alliance] The Trader War",
            "  S141SC0000 [lvl 1, 96x96, 12 cities, Average, Ethan Petty] [Human/Empire, Heretic/Legions of the Damned, Undead/Undead Hordes, Elf/Elven Alliance] Dividing Line",
            "  S142SC0000 [lvl 1, 120x120, 12 cities, Average, Ethan Petty] [Human/Empire, Dwarf/Mountain Clans, Undead/Undead Hordes, Elf/Elven Alliance] Gallean's Promise"
        ]));
    }

    [Test]
    public void CampaignCatalogFromIndexTest()
    {
        GlobalDatabaseIndex globalIndex = GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabaseIndex scenarioIndex = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);

        IReadOnlyList<ScenarioCampaign> fromIndex = ScenarioLoader.EnumerateCampaigns(globalIndex, scenarioIndex, campaignDb);
        IReadOnlyList<ScenarioCampaign> fromDatabase = ScenarioLoader.EnumerateCampaigns(globalDb, scenarioDb, campaignDb);

        Assert.That(FormatCampaigns(fromIndex, texts), Is.EqualTo(FormatCampaigns(fromDatabase, texts)));
    }

    [Test]
    public void CampaignScenariosTest()
    {
        IReadOnlyList<ScenarioCampaign> campaigns = ScenarioLoader.EnumerateCampaigns(globalDb, scenarioDb, campaignDb);

        ScenarioCampaign campaign = campaigns[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(campaign.Id, Is.EqualTo(new ScenarioCampaignId("C000CC0002")));
            Assert.That(campaign.NameTextId, Is.EqualTo(new GlobalTextId("X000TAE001")));
            Assert.That(campaign.PlayRace, Is.EqualTo(new ScenarioRace(WorldRace.Human, new GlobalTextId("X000TG0000"))));
            Assert.That(campaign.IsSkirmish, Is.False);
            Assert.That(campaign.Scenarios.Select(scenario => scenario with { PlayableRaces = [] }), Is.EqualTo([
                new ScenarioMetadata(
                    new ScenarioId("S107SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S107IF0000:NAME:0"),
                        new ScenarioTextId("S107IF0000:DESC:0"),
                        new ScenarioTextId("S107IF0000:BRIEFING:0"),
                        new ScenarioTextId("S107IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S107IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S107IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S107IF0000:CREATOR:0")),
                    1,
                    48,
                    5,
                    WorldDifficulty.Easy,
                    PlayableRaces: []),
                new ScenarioMetadata(
                    new ScenarioId("S108SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S108IF0000:NAME:0"),
                        new ScenarioTextId("S108IF0000:DESC:0"),
                        new ScenarioTextId("S108IF0000:BRIEFING:0"),
                        new ScenarioTextId("S108IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S108IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S108IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S108IF0000:CREATOR:0")),
                    1,
                    72,
                    7,
                    WorldDifficulty.Easy,
                    PlayableRaces: []),
                new ScenarioMetadata(
                    new ScenarioId("S095SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S095IF0000:NAME:0"),
                        new ScenarioTextId("S095IF0000:DESC:0"),
                        new ScenarioTextId("S095IF0000:BRIEFING:0"),
                        new ScenarioTextId("S095IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S095IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S095IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S095IF0000:CREATOR:0")),
                    1,
                    48,
                    4,
                    WorldDifficulty.Easy,
                    PlayableRaces: []),
                new ScenarioMetadata(
                    new ScenarioId("S096SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S096IF0000:NAME:0"),
                        new ScenarioTextId("S096IF0000:DESC:0"),
                        new ScenarioTextId("S096IF0000:BRIEFING:0"),
                        new ScenarioTextId("S096IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S096IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S096IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S096IF0000:CREATOR:0")),
                    1,
                    72,
                    8,
                    WorldDifficulty.Easy,
                    PlayableRaces: []),
                new ScenarioMetadata(
                    new ScenarioId("S097SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S097IF0000:NAME:0"),
                        new ScenarioTextId("S097IF0000:DESC:0"),
                        new ScenarioTextId("S097IF0000:BRIEFING:0"),
                        new ScenarioTextId("S097IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S097IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S097IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S097IF0000:CREATOR:0")),
                    1,
                    72,
                    8,
                    WorldDifficulty.Easy,
                    PlayableRaces: []),
                new ScenarioMetadata(
                    new ScenarioId("S098SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S098IF0000:NAME:0"),
                        new ScenarioTextId("S098IF0000:DESC:0"),
                        new ScenarioTextId("S098IF0000:BRIEFING:0"),
                        new ScenarioTextId("S098IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S098IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S098IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S098IF0000:CREATOR:0")),
                    1,
                    72,
                    8,
                    WorldDifficulty.Easy,
                    PlayableRaces: []),
                new ScenarioMetadata(
                    new ScenarioId("S099SC0000"),
                    new ScenarioInfoTexts(
                        new ScenarioTextId("S099IF0000:NAME:0"),
                        new ScenarioTextId("S099IF0000:DESC:0"),
                        new ScenarioTextId("S099IF0000:BRIEFING:0"),
                        new ScenarioTextId("S099IF0000:BRIEFLONG:0"),
                        new ScenarioTextId("S099IF0000:DEBUNKW:0"),
                        new ScenarioTextId("S099IF0000:DEBUNKL:0"),
                        new ScenarioTextId("S099IF0000:CREATOR:0")),
                    1,
                    96,
                    13,
                    WorldDifficulty.Easy,
                    PlayableRaces: [])
            ]));
        }
    }

    [Test]
    public void MissingDescriptionTest()
    {
        IReadOnlyList<ScenarioCampaign> campaigns = ScenarioLoader.EnumerateCampaigns(globalDb, scenarioDb, campaignDb);

        ScenarioCampaign campaign = campaigns[5];

        Assert.That(campaign.Scenarios.Select(scenario => scenario with { PlayableRaces = [] }), Is.EqualTo([
            new ScenarioMetadata(
                new ScenarioId("S135SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S135IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S135IF0000:BRIEFING:0"),
                    new ScenarioTextId("S135IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S135IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S135IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S135IF0000:CREATOR:0")),
                1,
                48,
                3,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S136SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S136IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S136IF0000:BRIEFING:0"),
                    new ScenarioTextId("S136IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S136IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S136IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S136IF0000:CREATOR:0")),
                1,
                48,
                4,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S137SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S137IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S137IF0000:BRIEFING:0"),
                    new ScenarioTextId("S137IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S137IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S137IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S137IF0000:CREATOR:0")),
                1,
                48,
                1,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S138SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S138IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S138IF0000:BRIEFING:0"),
                    new ScenarioTextId("S138IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S138IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S138IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S138IF0000:CREATOR:0")),
                1,
                72,
                10,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S139SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S139IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S139IF0000:BRIEFING:0"),
                    new ScenarioTextId("S139IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S139IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S139IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S139IF0000:CREATOR:0")),
                1,
                72,
                8,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S140SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S140IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S140IF0000:BRIEFING:0"),
                    new ScenarioTextId("S140IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S140IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S140IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S140IF0000:CREATOR:0")),
                1,
                48,
                3,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S141SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S141IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S141IF0000:BRIEFING:0"),
                    new ScenarioTextId("S141IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S141IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S141IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S141IF0000:CREATOR:0")),
                1,
                96,
                12,
                WorldDifficulty.Average,
                PlayableRaces: []),
            new ScenarioMetadata(
                new ScenarioId("S142SC0000"),
                new ScenarioInfoTexts(
                    new ScenarioTextId("S142IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S142IF0000:BRIEFING:0"),
                    new ScenarioTextId("S142IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S142IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S142IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S142IF0000:CREATOR:0")),
                1,
                120,
                12,
                WorldDifficulty.Average,
                PlayableRaces: [])
        ]));
    }
}
