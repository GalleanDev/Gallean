namespace Gallean.Assets;

public sealed class AssetPackSource<TPixels> : IAssetSource<TPixels>
{
    private readonly IAssetDecoder<TPixels> decoder;
    private readonly IReadOnlyList<AssetPack> packs;
    private readonly IReadOnlyDictionary<string, (AssetPack Pack, AssetManifestAnimation Animation)> animations;
    private readonly IReadOnlyDictionary<string, (AssetPack Pack, AssetManifestSound Sound)> sounds;
    private readonly IReadOnlyDictionary<string, (AssetPack Pack, AssetManifestSprite Sprite)> sprites;

    public AssetPackSource(IReadOnlyList<AssetPack> packs, IAssetDecoder<TPixels> decoder)
    {
        this.packs = packs;
        this.decoder = decoder;

        Dictionary<string, (AssetPack, AssetManifestSprite)> spriteLookup = [];
        Dictionary<string, (AssetPack, AssetManifestAnimation)> animationLookup = [];
        Dictionary<string, (AssetPack, AssetManifestSound)> soundLookup = [];

        foreach (AssetPack pack in packs)
        {
            foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
            {
                spriteLookup[sprite.Id] = (pack, sprite);
            }

            foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
            {
                animationLookup[animation.Id] = (pack, animation);
            }

            foreach (AssetManifestSound sound in pack.Manifest.Sounds)
            {
                soundLookup[sound.Id] = (pack, sound);
            }
        }

        sprites = spriteLookup;
        animations = animationLookup;
        sounds = soundLookup;
    }

    public AssetSprite<TPixels>? TryGetSprite(AssetSpriteKey key)
    {
        if (sprites.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSprite Sprite) found))
        {
            return Decode(found.Pack, found.Sprite);
        }
        else
        {
            return null;
        }
    }

    public AssetSpriteMetadata? TryGetSpriteMetadata(AssetSpriteKey key)
    {
        if (sprites.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSprite Sprite) found))
        {
            return found.Sprite.Metadata;
        }
        else
        {
            return null;
        }
    }

    public AssetSpriteLayer<TPixels>? TryGetSpriteLayer(AssetSpriteKey key, AssetSpriteLayerType type)
    {
        if (!sprites.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSprite Sprite) found))
        {
            return null;
        }

        AssetManifestSpriteLayer? layer = FindSpriteLayer(found.Sprite.Layers, type);

        if (layer is null)
        {
            return null;
        }

        return Decode(found.Pack, layer);
    }

    public IReadOnlyList<AssetSpriteLayer<TPixels>>? TryGetAnimationLayer(AssetAnimationKey key, AssetSpriteLayerType type)
    {
        if (!animations.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestAnimation Animation) found))
        {
            return null;
        }

        List<AssetSpriteLayer<TPixels>> frames = [];

        foreach (AssetManifestAnimationFrame frame in found.Animation.Frames)
        {
            AssetManifestSpriteLayer? layer = FindSpriteLayer(frame.Layers, type);

            if (layer is null)
            {
                return null;
            }

            frames.Add(Decode(found.Pack, layer));
        }

        return frames;
    }

    public AssetAnimation<TPixels>? TryGetAnimation(AssetAnimationKey key)
    {
        if (animations.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestAnimation Animation) found))
        {
            List<AssetSprite<TPixels>> frames = [];

            foreach (AssetManifestAnimationFrame frame in found.Animation.Frames)
            {
                frames.Add(Decode(found.Pack, frame));
            }

            return new AssetAnimation<TPixels>(found.Animation.Metadata, frames);
        }
        else
        {
            return null;
        }
    }

    public AssetAnimationMetadata? TryGetAnimationMetadata(AssetAnimationKey key)
    {
        if (animations.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestAnimation Animation) found))
        {
            return found.Animation.Metadata;
        }
        else
        {
            return null;
        }
    }

    public AssetSound? TryGetSound(AssetSoundKey key)
    {
        if (sounds.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSound Sound) found))
        {
            return new AssetSound(found.Sound.Metadata, found.Pack.OpenSound(found.Sound));
        }
        else
        {
            return null;
        }
    }

    public AssetSoundMetadata? TryGetSoundMetadata(AssetSoundKey key)
    {
        if (sounds.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSound Sound) found))
        {
            return found.Sound.Metadata;
        }
        else
        {
            return null;
        }
    }

    public AssetStream? TryGetStream(AssetKey key)
    {
        if (sprites.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSprite Sprite) sprite))
        {
            return sprite.Pack.OpenSprite(sprite.Sprite);
        }

        if (sounds.TryGetValue(key.CanonicalId, out (AssetPack Pack, AssetManifestSound Sound) sound))
        {
            return sound.Pack.OpenSound(sound.Sound);
        }

        return null;
    }

    public void Dispose()
    {
        foreach (AssetPack pack in packs)
        {
            pack.Dispose();
        }
    }

    private AssetSprite<TPixels> Decode(AssetPack pack, AssetManifestSprite sprite)
    {
        return new AssetSprite<TPixels>(sprite.Metadata, decoder.DecodeFrame(sprite.Metadata, pack.ReadSprite(sprite)));
    }

    private AssetSprite<TPixels> Decode(AssetPack pack, AssetManifestAnimationFrame frame)
    {
        return new AssetSprite<TPixels>(frame.Metadata, decoder.DecodeFrame(frame.Metadata, pack.ReadAnimationFrame(frame)));
    }

    private AssetSpriteLayer<TPixels> Decode(AssetPack pack, AssetManifestSpriteLayer layer)
    {
        TPixels pixels = decoder.DecodeLayer(layer.PixelWidth, layer.PixelHeight, pack.ReadSpriteLayer(layer));

        return new AssetSpriteLayer<TPixels>(layer.PixelWidth, layer.PixelHeight, pixels);
    }

    private static AssetManifestSpriteLayer? FindSpriteLayer(IReadOnlyList<AssetManifestSpriteLayer> layers, AssetSpriteLayerType type)
    {
        foreach (AssetManifestSpriteLayer candidate in layers)
        {
            if (candidate.Type == type)
            {
                return candidate;
            }
        }

        return null;
    }
}
