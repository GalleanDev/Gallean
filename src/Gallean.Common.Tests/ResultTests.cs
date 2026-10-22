namespace Gallean.Common.Tests;

public sealed class ResultTests
{
    [Test]
    public void ValueTest()
    {
        Result<string> result = Result<string>.FromValue("value");

        Assert.That(result.IsSuccessful, Is.True);
        Assert.That(result.Value, Is.EqualTo("value"));
    }

    [Test]
    public void ExceptionTest()
    {
        InvalidOperationException error = new("Failed.");
        Result<string> result = Result<string>.FromException(error);

        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(() => result.Value, Throws.Exception.SameAs(error));
    }

    [Test]
    public void EqualityTest()
    {
        Result<string> result = Result<string>.FromValue("value");

        Assert.That(result, Is.EqualTo(Result<string>.FromValue("value")));
        Assert.That(result, Is.Not.EqualTo(Result<string>.FromValue("other")));
        Assert.That(result, Is.Not.EqualTo(Result<string>.FromException(new InvalidOperationException("Failed."))));
    }
}
