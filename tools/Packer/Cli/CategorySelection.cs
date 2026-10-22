using System.Diagnostics.CodeAnalysis;

namespace Packer;

internal sealed record CategorySelection(CategorySelector? Selector)
{
    public static bool TryCreate(
        IReadOnlyList<string> categories,
        PackerOutput output,
        [NotNullWhen(true)] out CategorySelection? selection)
    {
        List<string> patterns = [];

        foreach (string category in categories)
        {
            if (!CategoryAliases.IsAlias(category))
            {
                patterns.Add(category);
            }
            else if (CategoryAliases.TryResolve(category, out IReadOnlyList<string>? aliasPatterns))
            {
                patterns.AddRange(aliasPatterns);
            }
            else
            {
                output.WriteError(
                    $"Unknown category alias '{category}'. " +
                    $"Known aliases: {string.Join(", ", CategoryAliases.Names)}.");

                selection = null;

                return false;
            }
        }

        selection = new CategorySelection(CreateSelector(patterns));

        return true;
    }

    private static CategorySelector? CreateSelector(IReadOnlyList<string> patterns)
    {
        if (patterns.Count > 0)
        {
            return CategorySelector.FromPatterns(patterns);
        }
        else
        {
            return null;
        }
    }
}
