namespace Gallean.Engine;

internal static class UnitDefinitionExtensions
{
    extension(UnitDefinition definition)
    {
        public bool HasAttackType(UnitAttackType type)
        {
            return definition.Attack.Type == type || definition.SecondaryAttack?.Type == type;
        }

        public bool HasAnyAttackType(params ReadOnlySpan<UnitAttackType> types)
        {
            foreach (UnitAttackType type in types)
            {
                if (definition.HasAttackType(type))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
