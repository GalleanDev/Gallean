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
        WorldScenarioPopupEvent popup)
    {
        string scenarioText = texts.GetScenarioText(popup.TextId);
        ScenarioPopupText popupText = ScenarioPopupText.Parse(scenarioText);
        ImmutableArray<ScenarioTextSpan> spans = formatter.Format(popupText.Speech);

        if (popup.PortraitId is { } portraitId)
        {
            string portraitName = popupText.Speaker ?? popup.PortraitName ?? ResolveUnitName(portraitId, definitions, texts);

            return new ScenarioEventPopupMessage(spans, portraitName, new AssetSpriteKey.UnitEventPortrait(portraitId), popup.IsPortraitLeft);
        }
        else
        {
            return new ScenarioEventPopupMessage(spans, popupText.Speaker ?? string.Empty, PortraitKey: null, popup.IsPortraitLeft);
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
