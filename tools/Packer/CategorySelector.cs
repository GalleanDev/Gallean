namespace Packer;

internal sealed partial class CategorySelector
{
    private readonly IReadOnlyList<Pattern> patterns;

    private CategorySelector(IReadOnlyList<Pattern> patterns)
    {
        this.patterns = patterns;
    }

    public static CategorySelector FromPatterns(IEnumerable<string> patterns)
    {
        List<Pattern> result = [];

        foreach (string pattern in patterns)
        {
            result.Add(new Pattern(pattern));
        }

        return new CategorySelector(result);
    }

    public bool Matches(string canonicalId)
    {
        foreach (Pattern pattern in patterns)
        {
            if (pattern.Matches(canonicalId))
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<string> UnmatchedPatterns(IEnumerable<string> canonicalIds)
    {
        HashSet<Pattern> matched = [];

        foreach (string id in canonicalIds)
        {
            foreach (Pattern pattern in patterns)
            {
                if (pattern.Matches(id))
                {
                    matched.Add(pattern);
                }
            }
        }

        List<string> unmatched = [];

        foreach (Pattern pattern in patterns)
        {
            if (!matched.Contains(pattern))
            {
                unmatched.Add(pattern.Text);
            }
        }

        return unmatched;
    }
}
