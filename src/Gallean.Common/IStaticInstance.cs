namespace Gallean.Common;

public interface IStaticInstance<TSelf> where TSelf : IStaticInstance<TSelf>, new();
