namespace Gallean.Engine.Contracts;

public readonly record struct WorldResourceSet(
    int Gold,
    int DeathMana,
    int RuneMana,
    int LifeMana,
    int InfernalMana,
    int GroveMana)
{
    public static WorldResourceSet Empty { get; } = new(0, 0, 0, 0, 0, 0);

    public override string ToString()
    {
        if (this == Empty)
        {
            return "Empty";
        }
        else
        {
            return $"G{Gold}:DM{DeathMana}:RM{RuneMana}:LM{LifeMana}:IM{InfernalMana}:GM{GroveMana}";
        }
    }
}
