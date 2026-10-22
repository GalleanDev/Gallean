namespace Gallean.Engine;

internal static partial class StackAutoFitCalculator
{
    private sealed record Selection(
        IReadOnlyList<Candidate> Candidates,
        int Size,
        int Gold,
        int Experience,
        int Hitpoints,
        int StackUnits) : IComparable<Selection>
    {
        public int CompareTo(Selection? other)
        {
            if (other is null)
            {
                return 1;
            }

            int rv;

            rv = other.Gold.CompareTo(Gold);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.Experience.CompareTo(Experience);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.Hitpoints.CompareTo(Hitpoints);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.StackUnits.CompareTo(StackUnits);
            if (rv != 0)
            {
                return rv;
            }

            int commonLength = Math.Min(Candidates.Count, other.Candidates.Count);

            for (int index = 0; index < commonLength; index++)
            {
                rv = Candidates[index].CompareTo(other.Candidates[index]);
                if (rv != 0)
                {
                    return rv;
                }
            }

            rv = Candidates.Count.CompareTo(other.Candidates.Count);
            if (rv != 0)
            {
                return rv;
            }

            return 0;
        }
    }
}
