using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [JsonConverter(typeof(IdJsonConverterFactory))]
    public readonly record struct Id(string Value)
    {
        public IdCategory Category
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Value.Length < 1)
                {
                    return IdCategory.Invalid;
                }

                return char.ToUpperInvariant(Value[0]) switch
                {
                    'G' => IdCategory.Global,
                    'C' => IdCategory.Campaign,
                    'S' => IdCategory.Scenario,
                    'E' => IdCategory.External,
                    _ => IdCategory.Invalid
                };
            }
        }

        public int CategoryIndex
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Value.Length < 4)
                {
                    return -1;
                }

                if (int.TryParse(Value.AsSpan(1, 3), out int index))
                {
                    return index;
                }

                return -1;
            }
        }

        public string TypeCode
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Value.Length < 6)
                {
                    return string.Empty;
                }

                return Value[4..6].ToUpperInvariant();
            }
        }

        public IdType Type
        {
            get
            {
                return TypeCode switch
                {
                    "TA" => IdType.ApplicationText,
                    "BB" => IdType.Building,
                    "RR" => IdType.Race,
                    "LR" => IdType.Lord,
                    "SS" => IdType.Spell,
                    "UU" => IdType.UnitGlobal,
                    "UG" => IdType.UnitGenerated,
                    "UM" => IdType.UnitModifier,
                    "AA" => IdType.Attack,
                    "TG" => IdType.TextGlobal,
                    "MG" => IdType.LandmarkGlobal,
                    "IG" => IdType.ItemGlobal,
                    "NA" => IdType.NobleAction,
                    "DU" => IdType.DynamicUpgrade,
                    "DA" => IdType.DynamicAttack,
                    "AL" => IdType.DynamicAltAttack,
                    "DC" => IdType.DynamicAttack2,
                    "AC" => IdType.DynamicAltAttack2,
                    "CC" => IdType.CampaignFile,
                    "CW" => IdType.CW,
                    "CO" => IdType.CO,
                    "PN" => IdType.Plan,
                    "OB" => IdType.ObjectCount,
                    "SC" => IdType.ScenarioFile,
                    "MP" => IdType.Map,
                    "MB" => IdType.MapBlock,
                    "IF" => IdType.ScenarioInfo,
                    "ET" => IdType.SpellEffects,
                    "FT" => IdType.Fortification,
                    "PL" => IdType.Player,
                    "KS" => IdType.PlayerKnownSpells,
                    "FG" => IdType.Fog,
                    "PB" => IdType.PlayerBuildings,
                    "RA" => IdType.Road,
                    "KC" => IdType.Stack,
                    "UN" => IdType.Unit,
                    "MM" => IdType.Landmark,
                    "IM" => IdType.Item,
                    "BG" => IdType.Bag,
                    "SI" => IdType.Site,
                    "RU" => IdType.Ruin,
                    "TB" => IdType.Tomb,
                    "RD" => IdType.Rod,
                    "CR" => IdType.Crystal,
                    "DP" => IdType.Diplomacy,
                    "ST" => IdType.SpellCast,
                    "LO" => IdType.Location,
                    "TM" => IdType.StackTemplate,
                    "EV" => IdType.Event,
                    "SD" => IdType.StackDestroyed,
                    "TC" => IdType.TalismanCharges,
                    "MT" => IdType.MT,
                    "ML" => IdType.Mountains,
                    "SR" => IdType.Subrace,
                    "BR" => IdType.SubraceType,
                    "QL" => IdType.QuestLog,
                    "TS" => IdType.TurnSummary,
                    "SV" => IdType.ScenarioVariable,
                    _ => IdType.Invalid
                };
            }
        }

        public int TypeIndex
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Value.Length < 10)
                {
                    return -1;
                }

                try
                {
                    return Convert.ToInt32(Value[6..10], 16);
                }
                catch
                {
                    return -1;
                }
            }
        }

        public bool Equals(Id other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Value is not null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value) : 0;
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(Id id)
        {
            return id.Value;
        }

        public static implicit operator Id(string value)
        {
            return new Id(value);
        }
    }

    internal sealed class IdJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Id) || typeToConvert == typeof(Id?);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return typeToConvert == typeof(Id?) ? new IdNullableJsonConverter() : new IdJsonConverter();
        }
    }

    internal sealed class IdJsonConverter : JsonConverter<Id>
    {
        public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();

            if (value is null)
            {
                throw new JsonException("Unexpected null database ID value.");
            }
            else if (string.Equals(value, "G000000000", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException("Unexpected empty database ID value.");
            }

            return new Id(value);
        }

        public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }

    internal sealed class IdNullableJsonConverter : JsonConverter<Id?>
    {
        public override Id? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            string? value = reader.GetString();

            if (value is null)
            {
                return null;
            }
            else if (string.Equals(value, "G000000000", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            else
            {
                return new Id(value);
            }
        }

        public override void Write(Utf8JsonWriter writer, Id? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value.Value);
            }
        }
    }

    public enum IdCategory
    {
        Global = 0,
        Campaign = 1,
        Scenario = 2,
        External = 3,
        Invalid = 4
    }

    public enum IdType
    {
        ApplicationText,
        Building,
        Race,
        Lord,
        Spell,
        UnitGlobal,
        UnitGenerated,
        UnitModifier,
        Attack,
        TextGlobal,
        LandmarkGlobal,
        ItemGlobal,
        NobleAction,
        DynamicUpgrade,
        DynamicAttack,
        DynamicAltAttack,
        DynamicAttack2,
        DynamicAltAttack2,
        CampaignFile,
        CW,
        CO,
        Plan,
        ObjectCount,
        ScenarioFile,
        Map,
        MapBlock,
        ScenarioInfo,
        SpellEffects,
        Fortification,
        Player,
        PlayerKnownSpells,
        Fog,
        PlayerBuildings,
        Road,
        Stack,
        Unit,
        Landmark,
        Item,
        Bag,
        Site,
        Ruin,
        Tomb,
        Rod,
        Crystal,
        Diplomacy,
        SpellCast,
        Location,
        StackTemplate,
        Event,
        StackDestroyed,
        TalismanCharges,
        MT,
        Mountains,
        Subrace,
        SubraceType,
        QuestLog,
        TurnSummary,
        ScenarioVariable,
        Invalid
    }
}
