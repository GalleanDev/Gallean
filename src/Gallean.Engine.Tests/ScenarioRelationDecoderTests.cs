namespace Gallean.Engine.Tests;

public sealed class ScenarioRelationDecoderTests
{
    [Test]
    public void EmptyTest()
    {
        WorldRelationId relationId = new(WorldRace.Human, WorldRace.Dwarf);

        Assert.That(
            ScenarioRelationDecoder.DecodeRelation(relationId, 0x00000000),
            Is.EqualTo(new WorldRelation(relationId, 0, 0, Alliance: false, 0, PermanentWar: false, PermanentAlliance: false)));
    }

    [Test]
    public void AllianceTest()
    {
        WorldRelationId relationId = new(WorldRace.Human, WorldRace.Dwarf);

        Assert.That(
            ScenarioRelationDecoder.DecodeRelation(relationId, 0x0000e4c9),
            Is.EqualTo(new WorldRelation(relationId, 100, 100, Alliance: true, 1, PermanentWar: false, PermanentAlliance: false)));
    }

    [Test]
    public void PermanentAllianceTest()
    {
        WorldRelationId relationId = new(WorldRace.Human, WorldRace.Dwarf);

        Assert.That(
            ScenarioRelationDecoder.DecodeRelation(relationId, unchecked((int)0x8000e4c9)),
            Is.EqualTo(new WorldRelation(relationId, 100, 100, Alliance: true, 1, PermanentWar: false, PermanentAlliance: true)));
    }

    [Test]
    public void PermanentWarTest()
    {
        WorldRelationId relationId = new(WorldRace.Human, WorldRace.Elf);

        Assert.That(
            ScenarioRelationDecoder.DecodeRelation(relationId, 0x40008000),
            Is.EqualTo(new WorldRelation(relationId, 0, 0, Alliance: false, 1, PermanentWar: true, PermanentAlliance: false)));
    }

    [Test]
    public void DistinctValuesTest()
    {
        WorldRelationId relationId = new(WorldRace.Human, WorldRace.Dwarf);

        Assert.That(
            ScenarioRelationDecoder.DecodeRelation(relationId, 0x0004295a),
            Is.EqualTo(new WorldRelation(relationId, 45, 41, Alliance: false, 8, PermanentWar: false, PermanentAlliance: false)));
    }
}
