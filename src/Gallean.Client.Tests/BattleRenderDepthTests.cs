using Gallean.Configuration;

namespace Gallean.Client.Tests;

public sealed class BattleRenderDepthTests
{
    private static string FormatFootprint(BattlePosition battlePosition, GridPosition position)
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

        return $"{battlePosition.ToString().ToLowerInvariant()} {line}-{flank}";
    }

    private static IReadOnlyList<string> FormatDrawOrder()
    {
        List<(int Depth, string Part)> parts = [];

        foreach (BattlePosition battlePosition in Enum.GetValues<BattlePosition>())
        {
            foreach (GridPosition position in GridPosition.All)
            {
                BattleGridFootprint footprint = new(battlePosition, position, IsLarge: false);
                string name = FormatFootprint(battlePosition, position);

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
            "left back-top unit",
            "left back-top effect",
            "left front-top unit",
            "left front-top effect",
            "left back-center unit",
            "left back-center effect",
            "left front-center unit",
            "left front-center effect",
            "left back-bottom unit",
            "left back-bottom effect",
            "left front-bottom unit",
            "left front-bottom effect",
            "right front-top unit",
            "right front-top effect",
            "right back-top unit",
            "right back-top effect",
            "right front-center unit",
            "right front-center effect",
            "right back-center unit",
            "right back-center effect",
            "right front-bottom unit",
            "right front-bottom effect",
            "right back-bottom unit",
            "right back-bottom effect"
        ]));
    }
}
