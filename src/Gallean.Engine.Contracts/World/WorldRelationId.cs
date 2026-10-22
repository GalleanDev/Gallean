namespace Gallean.Engine.Contracts;

public readonly record struct WorldRelationId : IComparable<WorldRelationId>
{
    public WorldRelationId(WorldRace race, WorldRace otherRace)
    {
        if (race <= otherRace)
        {
            Race1 = race;
            Race2 = otherRace;
        }
        else
        {
            Race1 = otherRace;
            Race2 = race;
        }
    }

    public WorldRace Race1 { get; }
    public WorldRace Race2 { get; }

    public int CompareTo(WorldRelationId other)
    {
        int rv;

        rv = Race1.CompareTo(other.Race1);
        if (rv != 0)
        {
            return rv;
        }

        rv = Race2.CompareTo(other.Race2);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    public override string ToString()
    {
        return $"{Race1}-{Race2}";
    }
}
