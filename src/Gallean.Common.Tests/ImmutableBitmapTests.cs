namespace Gallean.Common.Tests;

public sealed class ImmutableBitmapTests
{
    private static IReadOnlyList<string> FormatBitmap(ImmutableBitmap bitmap)
    {
        List<string> rows = [];

        for (int y = 0; y < bitmap.Height; y++)
        {
            char[] row = new char[bitmap.Width];

            for (int x = 0; x < bitmap.Width; x++)
            {
                row[x] = bitmap.Get(x, y) ? '#' : '.';
            }

            rows.Add(new string(row));
        }

        return rows;
    }

    [Test]
    public void CreateTest()
    {
        ImmutableBitmap bitmap = ImmutableBitmap.Create(3, 3, false);

        Assert.That(FormatBitmap(bitmap), Is.EqualTo([
            "...",
            "...",
            "..."
        ]));
    }

    [Test]
    public void CreateFilledTest()
    {
        ImmutableBitmap bitmap = ImmutableBitmap.Create(3, 3, true);

        Assert.That(FormatBitmap(bitmap), Is.EqualTo([
            "###",
            "###",
            "###"
        ]));
    }

    [Test]
    public void BuilderSetTest()
    {
        ImmutableBitmap bitmap = ImmutableBitmap.Create(3, 3, false);
        ImmutableBitmap.Builder builder = bitmap.ToBuilder();

        builder.Set(1, 1);

        Assert.That(FormatBitmap(builder.ToImmutable()), Is.EqualTo([
            "...",
            ".#.",
            "..."
        ]));

        Assert.That(FormatBitmap(bitmap), Is.EqualTo([
            "...",
            "...",
            "..."
        ]));
    }

    [Test]
    public void BuilderClearTest()
    {
        ImmutableBitmap bitmap = ImmutableBitmap.Create(3, 3, true);
        ImmutableBitmap.Builder builder = bitmap.ToBuilder();

        builder.Clear(1, 1);

        Assert.That(FormatBitmap(builder.ToImmutable()), Is.EqualTo([
            "###",
            "#.#",
            "###"
        ]));

        Assert.That(FormatBitmap(bitmap), Is.EqualTo([
            "###",
            "###",
            "###"
        ]));
    }

    [Test]
    public void BuilderChangeAfterToImmutableTest()
    {
        ImmutableBitmap.Builder builder = ImmutableBitmap.Create(3, 3).ToBuilder();
        builder.Set(1, 1);
        ImmutableBitmap bitmap = builder.ToImmutable();

        builder.Clear(1, 1);

        Assert.That(FormatBitmap(bitmap), Is.EqualTo([
            "...",
            ".#.",
            "..."
        ]));
    }

    [Test]
    public void EqualityTest()
    {
        ImmutableBitmap bitmap1 = ImmutableBitmap.Create(3, 3, true);
        ImmutableBitmap bitmap2 = ImmutableBitmap.Create(3, 3, true);
        ImmutableBitmap bitmap3 = ImmutableBitmap.Create(3, 3, false);
        ImmutableBitmap bitmap4 = ImmutableBitmap.Create(4, 3, true);

        Assert.That(bitmap1, Is.Not.Null);
#pragma warning disable NUnit2009
        Assert.That(bitmap1, Is.EqualTo(bitmap1));
#pragma warning restore NUnit2009
        Assert.That(bitmap1, Is.EqualTo(bitmap2));
        Assert.That(bitmap1, Is.Not.EqualTo(bitmap3));
        Assert.That(bitmap1, Is.Not.EqualTo(bitmap4));
    }

    [Test]
    public void BitsRoundTripTest()
    {
        ImmutableBitmap.Builder builder = ImmutableBitmap.Create(5, 3).ToBuilder();
        builder.Set(0, 0);
        builder.Set(4, 0);
        builder.Set(2, 1);
        builder.Set(4, 2);
        ImmutableBitmap bitmap = builder.ToImmutable();

        byte[] bits = bitmap.ToBits();

        Assert.That(bits, Has.Length.EqualTo(2));
        Assert.That(new ImmutableBitmap.Builder(5, 3, bits).ToImmutable(), Is.EqualTo(bitmap));
    }

    [Test]
    public void BitsLengthMismatchTest()
    {
        Assert.Throws<ArgumentException>(() => new ImmutableBitmap.Builder(5, 3, [0, 0, 0]));
    }

    [Test]
    public void GetHashCodeTest()
    {
        ImmutableBitmap bitmap1 = ImmutableBitmap.Create(3, 3, true);
        ImmutableBitmap bitmap2 = ImmutableBitmap.Create(3, 3, true);

#pragma warning disable NUnit2009
        Assert.That(bitmap1.GetHashCode(), Is.EqualTo(bitmap1.GetHashCode()));
#pragma warning restore NUnit2009
        Assert.That(bitmap1.GetHashCode(), Is.EqualTo(bitmap2.GetHashCode()));
    }
}
