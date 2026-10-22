using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal static class OrnamentDecoration
{
    private const double CurlSize = 19.2;

    private static readonly Lazy<Geometry> NodeShape = new(CreateNode);
    private static readonly Lazy<Geometry> DropShape = new(CreateDrop);
    private static readonly Lazy<Geometry> CurlsShape = new(CreateCurls);
    private static readonly Lazy<Geometry> CrossShape = new(CreateCross);
    private static readonly Lazy<Geometry> EndShape = new(CreateEnd);
    private static readonly Lazy<Geometry> TipShape = new(CreateTip);

    public static Geometry Node => NodeShape.Value;
    public static Geometry Drop => DropShape.Value;
    public static Geometry Curls => CurlsShape.Value;
    public static Geometry Cross => CrossShape.Value;
    public static Geometry End => EndShape.Value;
    public static Geometry Tip => TipShape.Value;

    private static Geometry CreateGroup(params Geometry[] parts)
    {
        GeometryGroup group = new() { FillRule = FillRule.NonZero };

        foreach (Geometry part in parts)
        {
            group.Children.Add(part);
        }

        return group;
    }

    private static Geometry CreateTransformed(Geometry shape, Matrix transform)
    {
        return new GeometryGroup { FillRule = FillRule.NonZero, Transform = new MatrixTransform(transform), Children = { shape } };
    }

    private static Geometry CreateCurlPair(double gap)
    {
        Geometry curl = CreateGlyph(IconMoniker.OrnamentCurl);
        Matrix placement = OrnamentPlacement.At(new Point(gap, 0), CurlSize);

        return CreateGroup(
            CreateTransformed(curl, placement),
            CreateTransformed(curl, placement * OrnamentPlacement.MirrorHorizontally(0)));
    }

    private static Geometry CreateNode()
    {
        Geometry spike = OrnamentShape.Spike(3.2, 0.7);

        return CreateGroup(
            OrnamentShape.Diamond(),
            spike,
            CreateTransformed(spike, OrnamentPlacement.MirrorHorizontally(0)));
    }

    private static Geometry CreateDrop()
    {
        return CreateGroup(
            Node,
            CreateTransformed(OrnamentShape.Spike(3.0, 0.7), Matrix.CreateRotation(Math.PI / 2.0)),
            CreateTransformed(OrnamentShape.Diamond(), OrnamentPlacement.At(new Point(0.0, 3.6), 0.6)));
    }

    private static Geometry CreateCurls()
    {
        return CreateGroup(OrnamentShape.Diamond(), CreateCurlPair(1.6));
    }

    private static Geometry CreateCross()
    {
        return CreateGroup(CreateTransformed(Node, Matrix.CreateRotation(Math.PI / 2.0)), CreateCurlPair(1.2));
    }

    private static Geometry CreateEnd()
    {
        return CreateGroup(
            CreateTransformed(OrnamentShape.Spike(10, 1), Matrix.CreateTranslation(-10, 0)),
            CreateTransformed(OrnamentShape.Diamond(), OrnamentPlacement.At(new Point(1.6, 0.0), 0.9)));
    }

    private static Geometry CreateTip()
    {
        Geometry spike = OrnamentShape.Spike(2.2, 0.4);
        Matrix downward = Matrix.CreateRotation(Math.PI / 2.0);

        return CreateGroup(
            CreateTransformed(CreateGlyph(IconMoniker.OrnamentSpear), Matrix.CreateScale(4.36, 4.36)),
            CreateTransformed(spike, downward),
            CreateTransformed(spike, downward * OrnamentPlacement.MirrorVertically(0)));
    }

    public static Geometry CreateGlyph(IconMoniker moniker)
    {
        (IReadOnlyList<IconLayer> layers, double viewBox, Point anchor) = Icons.Get(moniker);
        Matrix transform = Matrix.CreateTranslation(-anchor.X, -anchor.Y) * Matrix.CreateScale(1.0 / viewBox, 1.0 / viewBox);

        List<Geometry> parts = [];

        foreach (IconLayer layer in layers)
        {
            parts.Add(CreateTransformed(layer.Geometry, transform));
        }

        return CreateGroup([.. parts]);
    }
}
