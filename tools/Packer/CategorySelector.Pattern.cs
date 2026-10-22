namespace Packer;

internal sealed partial class CategorySelector
{
    private sealed class Pattern(string text)
    {
        public string Text => text;

        public bool Matches(string canonicalId)
        {
            string[] segments = text.ToLowerInvariant().Split('/');
            string[] matchSegments = canonicalId.ToLowerInvariant().Split('/');

            if (segments.Length > matchSegments.Length)
            {
                return false;
            }

            for (int i = 0; i < segments.Length; i++)
            {
                if (!GlobMatch(segments[i], matchSegments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GlobMatch(string pattern, string text)
        {
            int p = 0;
            int t = 0;
            int star = -1;
            int mark = 0;

            while (t < text.Length)
            {
                if (p < pattern.Length && (pattern[p] == '?' || pattern[p] == text[t]))
                {
                    p++;
                    t++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    star = p;
                    mark = t;
                    p++;
                }
                else if (star != -1)
                {
                    mark++;
                    p = star + 1;
                    t = mark;
                }
                else
                {
                    return false;
                }
            }

            while (p < pattern.Length && pattern[p] == '*')
            {
                p++;
            }

            return p == pattern.Length;
        }
    }
}
