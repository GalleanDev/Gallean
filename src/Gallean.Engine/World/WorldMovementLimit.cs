namespace Gallean.Engine;

public sealed record WorldMovementLimit(
    int MovementPoints,
    int MaxMovementPoints,
    int TurnLimit)
{
    public static readonly WorldMovementLimit Unlimited = new(int.MaxValue, int.MaxValue, int.MaxValue);
}
