using System.Reflection;
using System.Text.Json.Serialization;

namespace Gallean.Engine.Tests;

public sealed class WorldStateJsonPolymorphismTests
{
    [TestCase(typeof(WorldFortification))]
    [TestCase(typeof(WorldSite))]
    [TestCase(typeof(WorldStackOrder))]
    [TestCase(typeof(WorldAiTarget))]
    public void DerivedTypesTest(Type type)
    {
        IEnumerable<Type> registered = type
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(attribute => attribute.DerivedType);
        IEnumerable<Type> derived = typeof(WorldState).Assembly
            .GetTypes()
            .Where(candidate => !candidate.IsAbstract && candidate.IsAssignableTo(type));

        Assert.That(registered, Is.EquivalentTo(derived));
    }
}
