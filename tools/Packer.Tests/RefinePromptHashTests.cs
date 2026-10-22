namespace Packer.Tests;

public sealed class RefinePromptHashTests
{
    [Test]
    public void EmptyPromptTest()
    {
        Assert.That(RefinePromptHash.Get("empty").ToString(), Is.EqualTo("e3b0c44298fc1c149afbf4c8"));
    }

    [Test]
    public void UnitPromptTest()
    {
        Assert.That(RefinePromptHash.Get("battle-unit/g000uu0001/attacker").Value.Length, Is.EqualTo(12));
    }

    [Test]
    public void MissingPromptTest()
    {
        KeyNotFoundException ex = Assert.Throws<KeyNotFoundException>(() => RefinePromptHash.Get("battle-unit/nothing/attacker"));

        Assert.That(ex.Message, Is.EqualTo("No prompt found for key 'battle-unit/nothing/attacker'."));
    }
}
