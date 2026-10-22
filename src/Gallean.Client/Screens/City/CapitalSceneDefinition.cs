using static Gallean.Client.CapitalSceneLayerDefinition;

namespace Gallean.Client;

internal sealed record CapitalSceneDefinition(IReadOnlyList<CapitalSceneLayerDefinition> Layers)
{
    private static readonly CapitalSceneDefinition Human = new(
    [
        new Background(0),
        new Background(1),
        new CapitalSceneLayerDefinition.Animation(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0012")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0015")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0022")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0014")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0016")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0017")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0018")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0019")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0020")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0021")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0024")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0025")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0005")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0004")),
        new Background(2),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0009")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0008")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0003")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0006")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0007")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0001")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0011")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0010")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0002")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0013")),
        new CapitalSceneLayerDefinition.Animation(1),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0023"))
    ]);

    private static readonly CapitalSceneDefinition Undead = new(
    [
        new Background(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0082")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0081")),
        new CapitalSceneLayerDefinition.Animation(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0076")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0078")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0080")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0077")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0089")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0090")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0088")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0087")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0086")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0085")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0083")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0084")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0091")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0097")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0096")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0094")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0092")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0093")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0075")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0099")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0098")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0095")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0079"))
    ]);

    private static readonly CapitalSceneDefinition Heretic = new(
    [
        new Background(0),
        new CapitalSceneLayerDefinition.Animation(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0052")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0051")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0050")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0055")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0056")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0057")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0058")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0059")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0060")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0061")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0062")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0063")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0064")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0065")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0066")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0067")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0068")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0069")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0070")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0072")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0073")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0054")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0053")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0074")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0071"))
    ]);

    private static readonly CapitalSceneDefinition Dwarf = new(
    [
        new Background(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0044")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0048")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0047")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0041")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0042")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0043")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0032")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0028")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0027")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0029")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0033")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0030")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0031")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0026")),
        new CapitalSceneLayerDefinition.Animation(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0049")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0046")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0040")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0037")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0039")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0035")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0036")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0034")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0038")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0045"))
    ]);

    private static readonly CapitalSceneDefinition Elf = new(
    [
        new Background(0),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0113")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0116")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0117")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0114")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0105")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0120")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0119")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0118")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0111")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0110")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0112")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0109")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0107")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0108")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0100")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0103")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0101")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0106")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0122")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0121")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0104")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0102")),
        new Building(new WorldPlayerBuildingDefinitionId("G000BB0115"))
    ]);

    public static CapitalSceneDefinition? GetOptional(WorldRace race)
    {
        return race switch
        {
            WorldRace.Human => Human,
            WorldRace.Undead => Undead,
            WorldRace.Heretic => Heretic,
            WorldRace.Dwarf => Dwarf,
            WorldRace.Elf => Elf,
            WorldRace.Neutral => null
        };
    }
}
