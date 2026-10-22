namespace Gallean.Entities.Tests;

public sealed class DbScenarioIdTests
{
    [Test]
    public void EqualityTest()
    {
        Db.ScenarioId upperId = new("S095SC0000");
        Db.ScenarioId lowerId = new("s095sc0000");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upperId, Is.EqualTo(lowerId));
            Assert.That(upperId.GetHashCode(), Is.EqualTo(lowerId.GetHashCode()));
        }
    }
}
