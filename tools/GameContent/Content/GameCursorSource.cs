using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameCursorSource(GameImages cursors, GameImages battleImages)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors()
    {
        foreach (AssetAnimationKey.CursorType type in Enum.GetValues<AssetAnimationKey.CursorType>())
        {
            AssetAnimationKey.Cursor key = new(type);

            if (key.Arrow == key || key.Overlay == key)
            {
                yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.Cursors.GetCursorFrames(key.Type)));
            }
        }
    }

    private IReadOnlyList<AssetSprite>? GetCursorFrames(AssetAnimationKey.CursorType type)
    {
        Cursors.CursorImage image = Cursors.Image(type);

        if (image.IsBattle)
        {
            return GetFrames(battleImages, Cursors.Animated(image.Name)) ?? GetFrames(battleImages, image.Name);
        }
        else
        {
            return GetFrames(cursors, image.Name);
        }
    }

    private static IReadOnlyList<AssetSprite>? GetFrames(GameImages images, string name)
    {
        IReadOnlyList<AssetSprite>? frames = images.TryGetAnchoredAnimationFrames(name);

        if (frames is not null)
        {
            return frames;
        }

        AssetSprite? sprite = images.TryGetAnchoredSprite(name);

        if (sprite is not null)
        {
            return [sprite];
        }
        else
        {
            return null;
        }
    }
}
