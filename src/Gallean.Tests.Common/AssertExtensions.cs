using NUnit.Framework;

namespace Gallean.Tests.Common;

public static class AssertExtensions
{
    extension(Assert)
    {
        public static T IsOfType<T>(object? value)
        {
            Assert.That(value, Is.InstanceOf<T>());

            return (T)value!;
        }

        public static T NotNull<T>(T? value) where T : class
        {
            Assert.That(value, Is.Not.Null);

            return value!;
        }

        public static T NotNull<T>(T? value) where T : struct
        {
            Assert.That(value, Is.Not.Null);

            return value!.Value;
        }
    }
}
