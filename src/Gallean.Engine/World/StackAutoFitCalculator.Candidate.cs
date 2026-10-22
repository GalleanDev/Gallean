namespace Gallean.Engine;

internal static partial class StackAutoFitCalculator
{
    private sealed record Candidate(
        WorldUnit Unit,
        UnitDefinition Definition,
        bool IsFromStack,
        int Size) : IComparable<Candidate>
    {
        public int CompareTo(Candidate? other)
        {
            if (other is null)
            {
                return 1;
            }

            int rv;

            rv = other.Definition.IsLarge.CompareTo(Definition.IsLarge);
            if (rv != 0)
            {
                return rv;
            }

            rv = Unit.DefinitionId.CompareTo(other.Unit.DefinitionId);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.Unit.Hitpoints.CompareTo(Unit.Hitpoints);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.Unit.Experience.CompareTo(Unit.Experience);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.Unit.Level.CompareTo(Unit.Level);
            if (rv != 0)
            {
                return rv;
            }

            int commonLength = Math.Min(Unit.Modifiers.Length, other.Unit.Modifiers.Length);

            for (int index = 0; index < commonLength; index++)
            {
                rv = Unit.Modifiers[index].DefinitionId.CompareTo(other.Unit.Modifiers[index].DefinitionId);
                if (rv != 0)
                {
                    return rv;
                }

                rv = Nullable.Compare(Unit.Modifiers[index].RemainingTurns, other.Unit.Modifiers[index].RemainingTurns);
                if (rv != 0)
                {
                    return rv;
                }
            }

            rv = Unit.Modifiers.Length.CompareTo(other.Unit.Modifiers.Length);
            if (rv != 0)
            {
                return rv;
            }

            rv = other.IsFromStack.CompareTo(IsFromStack);
            if (rv != 0)
            {
                return rv;
            }

            rv = Unit.Id.CompareTo(other.Unit.Id);
            if (rv != 0)
            {
                return rv;
            }

            return 0;
        }
    }
}
