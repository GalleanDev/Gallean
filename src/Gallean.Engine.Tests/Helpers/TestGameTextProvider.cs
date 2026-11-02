using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestGameTextProvider : IGameTextProvider, IStaticInstance<TestGameTextProvider>
{
    public ImmutableArray<ScenarioName> Names => [];

    public GlobalText GetGlobalText(GlobalTextId id)
    {
        return new GlobalText(id, id.Value, IsVerified: true, Context: null);
    }

    public string GetScenarioText(ScenarioTextId id)
    {
        return id.Value;
    }

    public string GetLeaderName(LeaderNameId id)
    {
        string name = (id.Race, id.IsMale) switch
        {
            (WorldRace.Human, true) => "Konrad",
            (WorldRace.Human, false) => "Zolian",
            (WorldRace.Undead, true) => "Nigosh",
            (WorldRace.Undead, false) => "Orissia",
            (WorldRace.Heretic, true) => "Ekrezem",
            (WorldRace.Heretic, false) => "Kisera",
            (WorldRace.Dwarf, true) => "Calderic",
            (WorldRace.Dwarf, false) => "Olga",
            (WorldRace.Neutral, _) => "Neutrals",
            (WorldRace.Elf, true) => "Ime'el",
            (WorldRace.Elf, false) => "Cillen"
        };

        return id.Index == 0 ? name : $"{name} {id.Index}";
    }

    public string GetUnitName(UnitName name)
    {
        return name.Accept(new UnitNameFormatter(this));
    }
}
