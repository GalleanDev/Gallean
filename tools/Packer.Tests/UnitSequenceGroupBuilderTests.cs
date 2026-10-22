using System.Text;
using Gallean.Common;

namespace Packer.Tests;

public sealed class UnitSequenceGroupBuilderTests
{
    private static List<UmbraFrame> MakeFrames(params string[] hashes)
    {
        UmbraSpriteMetadata metadata = new(1, 1, 1, 1, 0, 0, Additive: false);
        List<UmbraFrame> frames = [];

        foreach (string hash in hashes)
        {
            frames.Add(new UmbraFrame(new UmbraSpriteHash(Encoding.UTF8.GetBytes(hash)), new UmbraSprite(metadata, new byte[4])));
        }

        return frames;
    }

    private static List<string> FormatGroups(IReadOnlyList<UmbraSequenceGroup> groups)
    {
        List<string> rows = [];

        foreach (UmbraSequenceGroup group in groups)
        {
            JoinStringBuilder members = new(" | ");

            foreach (UmbraSequence sequence in group.Sequences)
            {
                JoinStringBuilder hashes = new(",");

                foreach (UmbraFrame frame in sequence.Frames)
                {
                    hashes.Add(Encoding.UTF8.GetString(frame.Hash.Value.Span));
                }

                members.Add($"{sequence.Id} {hashes}");
            }

            rows.Add($"{group.Id}: {members}");
        }

        return rows;
    }

    [Test]
    public void BuildTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/idle/attacker"] = MakeFrames("i1", "i2", "i3", "i4"),
            ["battle-unit/g000uu0001/attack/attacker"] = MakeFrames("a1", "a2")
        };

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["battle-unit/g000uu0001/attack/attacker", "battle-unit/g000uu0001/idle/attacker"],
                framesById.Keys,
                id => framesById[id])
        ];

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker: battle-unit/g000uu0001/idle/attacker i1,i2,i3,i4 | battle-unit/g000uu0001/attack/attacker a1,a2"
        ]));
    }

    [Test]
    public void UnselectedPoseTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/idle/attacker"] = MakeFrames("i1", "i2", "i3", "i4"),
            ["battle-unit/g000uu0001/hit/attacker"] = MakeFrames("h1")
        };

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["battle-unit/g000uu0001/hit/attacker"],
                framesById.Keys,
                id => framesById[id])
        ];

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker: battle-unit/g000uu0001/idle/attacker i1,i2,i3,i4 | battle-unit/g000uu0001/hit/attacker h1"
        ]));
    }

    [Test]
    public void MissingIdleTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/hit/attacker"] = MakeFrames("h1")
        };

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["battle-unit/g000uu0001/hit/attacker"],
                framesById.Keys,
                id => framesById[id])
        ];

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/hit/attacker: battle-unit/g000uu0001/hit/attacker h1"
        ]));
    }

    [Test]
    public void PoseOrderTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/idle/attacker"] = MakeFrames("i1", "i2", "i3"),
            ["battle-unit/g000uu0001/attack/attacker"] = MakeFrames("a1")
        };

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["battle-unit/g000uu0001/attack/attacker"],
                framesById.Keys,
                id => framesById[id])
        ];

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker: battle-unit/g000uu0001/idle/attacker i1,i2,i3 | battle-unit/g000uu0001/attack/attacker a1"
        ]));
    }

    [Test]
    public void OtherSideTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/idle/defender"] = MakeFrames("i1", "i2"),
            ["battle-unit/g000uu0001/attack/attacker"] = MakeFrames("a1")
        };

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["battle-unit/g000uu0001/attack/attacker"],
                framesById.Keys,
                id => framesById[id])
        ];

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/attack/attacker: battle-unit/g000uu0001/attack/attacker a1"
        ]));
    }

    [Test]
    public void GroupOrderTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0002/attack/attacker"] = MakeFrames("b1"),
            ["battle-unit/g000uu0001/hit/attacker"] = MakeFrames("a3"),
            ["battle-unit/g000uu0001/idle/attacker"] = MakeFrames("a1"),
            ["battle-unit/g000uu0001/attack/attacker"] = MakeFrames("a2"),
            ["unit-face/g000uu0001"] = MakeFrames("f1")
        };

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                [
                    "unit-face/g000uu0001",
                    "battle-unit/g000uu0002/attack/attacker",
                    "battle-unit/g000uu0001/hit/attacker",
                    "battle-unit/g000uu0001/attack/attacker",
                    "battle-unit/g000uu0001/idle/attacker"
                ],
                framesById.Keys,
                id => framesById[id])
        ];

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker: battle-unit/g000uu0001/idle/attacker a1 | battle-unit/g000uu0001/attack/attacker a2 | battle-unit/g000uu0001/hit/attacker a3",
            "battle-unit/g000uu0002/attack/attacker: battle-unit/g000uu0002/attack/attacker b1",
            "unit-face/g000uu0001: unit-face/g000uu0001 f1"
        ]));
    }

    [Test]
    public void SharedIdleLoadTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/idle/attacker"] = MakeFrames("i1", "i2"),
            ["battle-unit/g000uu0001/attack/attacker"] = MakeFrames("a1"),
            ["battle-unit/g000uu0001/hit/attacker"] = MakeFrames("h1")
        };

        List<string> loads = [];

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["battle-unit/g000uu0001/attack/attacker", "battle-unit/g000uu0001/hit/attacker"],
                framesById.Keys,
                id =>
                {
                    loads.Add(id);

                    return framesById[id];
                })
        ];

        Assert.That(loads, Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker",
            "battle-unit/g000uu0001/attack/attacker",
            "battle-unit/g000uu0001/hit/attacker"
        ]));

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker: battle-unit/g000uu0001/idle/attacker i1,i2 | battle-unit/g000uu0001/attack/attacker a1 | battle-unit/g000uu0001/hit/attacker h1"
        ]));
    }

    [Test]
    public void UnusedIdleTest()
    {
        Dictionary<string, List<UmbraFrame>> framesById = new()
        {
            ["battle-unit/g000uu0001/idle/attacker"] = MakeFrames("i1"),
            ["unit-face/g000uu0001"] = MakeFrames("f1")
        };

        List<string> loads = [];

        List<UmbraSequenceGroup> groups =
        [
            .. UnitSequenceGroupBuilder.Build(
                ["unit-face/g000uu0001"],
                framesById.Keys,
                id =>
                {
                    loads.Add(id);

                    return framesById[id];
                })
        ];

        Assert.That(loads, Is.EqualTo(["unit-face/g000uu0001"]));

        Assert.That(FormatGroups(groups), Is.EqualTo([
            "unit-face/g000uu0001: unit-face/g000uu0001 f1"
        ]));
    }

    [Test]
    public void ExpandGroupsTest()
    {
        List<string> expanded = UnitSequenceGroupBuilder.ExpandGroups(
            ["battle-unit/g000uu0001/hit/attacker", "battle-unit/g000uu0001/attack/attacker"],
            [
                "battle-unit/g000uu0001/idle/attacker",
                "battle-unit/g000uu0001/attack/attacker",
                "battle-unit/g000uu0001/hit/attacker",
                "battle-unit/g000uu0001/idle/defender",
                "battle-unit/g000uu0002/idle/attacker",
                "unit-face/g000uu0001"
            ]);

        Assert.That(expanded, Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker",
            "battle-unit/g000uu0001/attack/attacker",
            "battle-unit/g000uu0001/hit/attacker"
        ]));
    }

    [Test]
    public void ExpandGroupsOtherEntryTest()
    {
        List<string> expanded = UnitSequenceGroupBuilder.ExpandGroups(
            ["unit-face/g000uu0001"],
            ["unit-face/g000uu0001", "battle-unit/g000uu0001/idle/attacker"]);

        Assert.That(expanded, Is.EqualTo(["unit-face/g000uu0001"]));
    }

    [Test]
    public void ExpandGroupsAbsentPoseTest()
    {
        List<string> expanded = UnitSequenceGroupBuilder.ExpandGroups(
            ["battle-unit/g000uu0001/hit/attacker"],
            ["battle-unit/g000uu0001/hit/attacker", "battle-unit/g000uu0001/idle/defender"]);

        Assert.That(expanded, Is.EqualTo(["battle-unit/g000uu0001/hit/attacker"]));
    }
}
