namespace Gallean.Engine.Tests;

public sealed class WorldStackOrderTests
{
    private static IEnumerable<Type> GetCanonicalOrders()
    {
        return typeof(WorldState).Assembly
            .GetTypes()
            .Where(candidate => !candidate.IsAbstract && candidate.IsAssignableTo(typeof(WorldCanonicalStackOrder)));
    }

    [Test]
    public void CanonicalOrdersTest()
    {
        Assert.That(GetCanonicalOrders().Select(order => order.Name), Is.EquivalentTo(Enum.GetNames<Db.OrderType>()));
    }
}
