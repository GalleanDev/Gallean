namespace Gallean.Common;

public static class StaticInstanceExtensions
{
    extension<TSelf>(TSelf) where TSelf : IStaticInstance<TSelf>, new()
    {
        public static TSelf Instance => InstanceData<TSelf>.Value;
    }

    private static class InstanceData<TSelf> where TSelf : IStaticInstance<TSelf>, new()
    {
        public static readonly TSelf Value = new();
    }
}
