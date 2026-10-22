namespace Gallean.Client.Tests;

public sealed class BattleRenderDepthTests
{
    private static string FormatFootprint(BattleSide side, GridPosition position)
    {
        string line;
        if (position.IsFrontLine)
        {
            line = "front";
        }
        else
        {
            line = "back";
        }

        string flank;
        switch (position.Flank)
        {
            case 0:
                flank = "bottom";
                break;
            case 1:
                flank = "center";
                break;
            default:
                flank = "top";
                break;
        }

        return $"{side.ToString().ToLowerInvariant()} {line}-{flank}";
    }

    private static IReadOnlyList<string> FormatDrawOrder()
    {
        List<(int Depth, string Part)> parts = [];

        foreach (BattleSide side in Enum.GetValues<BattleSide>())
        {
            foreach (GridPosition position in GridPosition.All)
            {
                BattleGridFootprint footprint = new(side, position, IsLarge: false);
                string name = FormatFootprint(side, position);

                parts.Add((BattleRenderDepth.GetUnitDepth(footprint), $"{name} unit"));
                parts.Add((BattleRenderDepth.GetEffectDepth(footprint), $"{name} effect"));
            }
        }

        return [.. parts.OrderBy(part => part.Depth).Select(part => part.Part)];
    }

    [Test]
    public void DrawOrderTest()
    {
        Assert.That(FormatDrawOrder(), Is.EqualTo([
            "attacker back-top unit",
            "attacker back-top effect",
            "attacker front-top unit",
            "attacker front-top effect",
            "attacker back-center unit",
            "attacker back-center effect",
            "attacker front-center unit",
            "attacker front-center effect",
            "attacker back-bottom unit",
            "attacker back-bottom effect",
            "attacker front-bottom unit",
            "attacker front-bottom effect",
            "defender front-top unit",
            "defender front-top effect",
            "defender back-top unit",
            "defender back-top effect",
            "defender front-center unit",
            "defender front-center effect",
            "defender back-center unit",
            "defender back-center effect",
            "defender front-bottom unit",
            "defender front-bottom effect",
            "defender back-bottom unit",
            "defender back-bottom effect"
        ]));
    }
}
