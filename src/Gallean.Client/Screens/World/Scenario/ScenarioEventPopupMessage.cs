using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record ScenarioEventPopupMessage(
    ImmutableArray<ScenarioTextSpan> Spans,
    string PortraitName,
    AssetSpriteKey? PortraitKey,
    bool IsPortraitLeft)
{
    public static ScenarioEventPopupMessage Create(
        IGameTextProvider texts,
        IGameDefinitionProvider definitions,
        ScenarioTextFormatter formatter,
        WorldScenarioMessage message)
    {
        string scenarioText = texts.GetScenarioText(message.TextId);
        ScenarioPopupText popupText = ScenarioPopupText.Parse(scenarioText);
        ImmutableArray<ScenarioTextSpan> spans = formatter.Format(popupText.Speech);

        if (message.PortraitId is not null)
        {
            string speakerName = popupText.Speaker ?? message.PortraitName ?? ResolveUnitName(message.PortraitId.Value, definitions, texts);

            return new ScenarioEventPopupMessage(spans, speakerName, new AssetSpriteKey.UnitEventPortrait(message.PortraitId.Value), message.IsPortraitLeft);
        }
        else
        {
            return new ScenarioEventPopupMessage(spans, popupText.Speaker ?? string.Empty, PortraitKey: null, message.IsPortraitLeft);
        }
    }

    public bool Equals(ScenarioEventPopupMessage? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Spans.SequenceEqual(other.Spans) &&
               PortraitName == other.PortraitName &&
               PortraitKey == other.PortraitKey &&
               IsPortraitLeft == other.IsPortraitLeft;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Spans);
        hash.Add(PortraitName);
        hash.Add(PortraitKey);
        hash.Add(IsPortraitLeft);

        return hash.ToHashCode();
    }

    private static string ResolveUnitName(
        ScenarioEventPortraitId portraitId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        UnitDefinition? definition = definitions.TryGetUnit(new UnitDefinitionId(portraitId.Value));

        if (definition is null)
        {
            return string.Empty;
        }

        return texts.GetGlobalText(definition.NameTextId).Text;
    }
}
