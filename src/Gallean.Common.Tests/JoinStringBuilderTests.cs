namespace Gallean.Common.Tests;

public sealed class JoinStringBuilderTests
{
    [Test]
    public void EmptyTest()
    {
        JoinStringBuilder sb = new(", ");

        Assert.That(sb.Count, Is.Zero);
        Assert.That(sb.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void SingleTest()
    {
        JoinStringBuilder sb = new(", ");

        sb.Add("a");

        Assert.That(sb.Count, Is.EqualTo(1));
        Assert.That(sb.ToString(), Is.EqualTo("a"));
    }

    [Test]
    public void AddTest()
    {
        JoinStringBuilder sb = new(", ");

        sb.Add("a");
        sb.Add("b");
        sb.Add("c");

        Assert.That(sb.Count, Is.EqualTo(3));
        Assert.That(sb.ToString(), Is.EqualTo("a, b, c"));
    }

    [Test]
    public void AddRangeTest()
    {
        JoinStringBuilder sb = new(" | ");

        sb.AddRange(["a", "b"]);
        sb.AddRange([]);
        sb.AddRange(["c"]);

        Assert.That(sb.Count, Is.EqualTo(3));
        Assert.That(sb.ToString(), Is.EqualTo("a | b | c"));
    }

    [Test]
    public void EmptyValuesTest()
    {
        JoinStringBuilder sb = new(",");

        sb.Add("");
        sb.Add("");

        Assert.That(sb.Count, Is.EqualTo(2));
        Assert.That(sb.ToString(), Is.EqualTo(","));
    }

    [Test]
    public void EmptySeparatorTest()
    {
        JoinStringBuilder sb = new("");

        sb.AddRange(["a", "b", "c"]);

        Assert.That(sb.ToString(), Is.EqualTo("abc"));
    }

    [Test]
    public void ChainTest()
    {
        string result = new JoinStringBuilder("+").Add("a").AddRange(["b", "c"]).Add("d").ToString();

        Assert.That(result, Is.EqualTo("a+b+c+d"));
    }
}
