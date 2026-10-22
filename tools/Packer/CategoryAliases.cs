using System.Diagnostics.CodeAnalysis;

namespace Packer;

internal static class CategoryAliases
{
    private const char Prefix = ':';

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Aliases =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["upscale-test-battle"] = UpscaleTestBattle()
        };

    public static IEnumerable<string> Names => Aliases.Keys;

    public static bool IsAlias(string pattern)
    {
        return pattern.StartsWith(Prefix);
    }

    public static bool TryResolve(string pattern, [NotNullWhen(true)] out IReadOnlyList<string>? patterns)
    {
        return Aliases.TryGetValue(pattern.TrimStart(Prefix), out patterns);
    }

    private static IReadOnlyList<string> UpscaleTestBattle()
    {
        return
        [
            "battle-background/terrain/neutral/1",

            "battle-unit/g000uu8016/idle/attacker",
            "battle-unit/g000uu8014/idle/attacker",
            "battle-unit/g000uu8015/idle/attacker",
            "battle-unit/g000uu8010/idle/attacker",
            "battle-unit/g000uu8032/idle/attacker",

            "battle-unit/g000uu0001/idle/defender",
            "battle-unit/g000uu5354/idle/defender",
            "battle-unit/g000uu0009/idle/defender",
            "battle-unit/g000uu0012/idle/defender",

            "battle-unit/g000uu8014/attack/attacker",
            "battle-unit/g000uu5354/hit/defender"
        ];
    }
}
