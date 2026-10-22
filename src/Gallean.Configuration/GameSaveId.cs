namespace Gallean.Configuration;

public readonly record struct GameSaveId(int Number)
{
    public override string ToString()
    {
        return Number.ToString();
    }
}
