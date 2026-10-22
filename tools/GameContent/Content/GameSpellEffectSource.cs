using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameSpellEffectSource(GameImages spellImages)
{
    private readonly record struct EffectKey(string Prefix, AssetAnimationKey.WorldSpellEffectLayer Layer, int Sublayer);

    private readonly record struct Effect(string Name, int Reveal);

    private Dictionary<EffectKey, Effect>? effects;

    public static IEnumerable<GameAssetDescriptor> ListDescriptors(IReadOnlyCollection<WorldSpellDefinitionId> spellIds)
    {
        foreach (WorldSpellDefinitionId spellId in spellIds)
        {
            foreach (AssetAnimationKey.WorldSpellEffectLayer layer in Enum.GetValues<AssetAnimationKey.WorldSpellEffectLayer>())
            {
                for (int sublayer = 0; sublayer < AssetAnimationKey.WorldSpellEffect.SublayerCount; sublayer++)
                {
                    AssetAnimationKey.WorldSpellEffect key = new(spellId, layer, sublayer);

                    yield return OptionalAnimation(key, source => source.SpellEffects.GetEffect(key.SpellId, key.Layer, key.Sublayer));
                }
            }
        }
    }

    public AssetAnimation? GetEffect(WorldSpellDefinitionId spellId, AssetAnimationKey.WorldSpellEffectLayer layer, int sublayer)
    {
        EffectKey key = new(Spells.Prefix(spellId), layer, sublayer);

        if (!GetEffects().TryGetValue(key, out Effect effect))
        {
            return null;
        }

        IReadOnlyList<AssetSprite>? frames = spellImages.TryGetAnimationFrames(effect.Name);

        if (frames is { Count: > 0 })
        {
            IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent(
                frames,
                SpriteOperations.ContentOrigin.Center,
                margin: 0);

            int? reveal;

            if (effect.Reveal > 0 && effect.Reveal < cropped.Count)
            {
                reveal = effect.Reveal;
            }
            else
            {
                reveal = null;
            }

            return AnimationOperations.Create(cropped, reveal, secondTriggerFrame: null);
        }

        return null;
    }

    private Dictionary<EffectKey, Effect> GetEffects()
    {
        if (effects is not null)
        {
            return effects;
        }

        Dictionary<EffectKey, Effect> resolved = [];

        foreach (string name in spellImages.GetAllAnimationNames())
        {
            if (Spells.TryParseEffectName(name, out AssetAnimationKey.WorldSpellEffectLayer layer, out int sublayer, out int reveal))
            {
                resolved[new EffectKey(name[..10].ToUpperInvariant(), layer, sublayer)] = new Effect(name, reveal);
            }
        }

        effects = resolved;

        return effects;
    }
}
