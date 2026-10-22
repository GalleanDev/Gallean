using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Gallean.Engine.Contracts;

namespace Gallean.Assets;

public abstract record AssetKey : IFormattable, IParsable<AssetKey>
{
    public abstract string CanonicalId { get; }

    public static AssetKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out AssetKey? key))
        {
            return key;
        }
        else
        {
            throw new FormatException($"The '{s}' asset key is not valid.");
        }
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out AssetKey result)
    {
        if (AssetSpriteKey.TryParse(s, provider, out AssetSpriteKey? spriteKey))
        {
            result = spriteKey;
            return true;
        }

        if (AssetAnimationKey.TryParse(s, provider, out AssetAnimationKey? animationKey))
        {
            result = animationKey;
            return true;
        }

        if (AssetSoundKey.TryParse(s, provider, out AssetSoundKey? soundKey))
        {
            result = soundKey;
            return true;
        }

        result = null;
        return false;
    }

    public sealed override string ToString()
    {
        return CanonicalId;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return CanonicalId;
    }

    protected static string Token(ScenarioId scenarioId)
    {
        return scenarioId.Value.ToLowerInvariant();
    }

    protected static string Token(UnitDefinitionId unitDefinitionId)
    {
        return unitDefinitionId.Value.ToLowerInvariant();
    }

    protected static string Token(ScenarioEventPortraitId portraitId)
    {
        return portraitId.Value.ToLowerInvariant();
    }

    protected static string Token(WorldSpellDefinitionId spellId)
    {
        return spellId.Value.ToLowerInvariant();
    }

    protected static string Token(WorldPlayerBuildingDefinitionId buildingId)
    {
        return buildingId.Value.ToLowerInvariant();
    }

    protected static string Token(WorldLandmarkDefinitionId landmarkId)
    {
        return landmarkId.Value.ToLowerInvariant();
    }

    protected static string Token(ItemDefinitionId itemId)
    {
        return itemId.Value.ToLowerInvariant();
    }

    protected static string Token(LordDefinitionId lordId)
    {
        return lordId.Value.ToLowerInvariant();
    }

    protected static string Token<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        string name = value.ToString();
        StringBuilder token = new(name.Length);

        foreach (char ch in name)
        {
            if (char.IsUpper(ch) && token.Length > 0)
            {
                token.Append('-');
            }

            token.Append(char.ToLowerInvariant(ch));
        }

        return token.ToString();
    }

    protected static TEnum ParseEnum<TEnum>(string token) where TEnum : struct, Enum
    {
        foreach (TEnum value in Enum.GetValues<TEnum>())
        {
            if (Token(value) == token)
            {
                return value;
            }
        }

        return default;
    }

    protected static int ParseInt(string token)
    {
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            return value;
        }
        else
        {
            return 0;
        }
    }
}
