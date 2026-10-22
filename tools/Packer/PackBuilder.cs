using System.Diagnostics;
using GameContent;
using Umbra.Backgrounds;

namespace Packer;

internal sealed class PackBuilder(
    IReadOnlyList<PackBuilderEntry> entries,
    IGameAssetReader reader,
    Action<PackBuilderProgress> reportProgress)
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(5);

    private static int MaxBatchEntries => Environment.ProcessorCount * 4;

    public PackBuilderResult Build(PackBuilderOptions options)
    {
        FrameEncoder.QuantizerPool quantizers = new();
        GameContentFilterSource source = new(entries, reader);
        List<string> missingRequired = [];

        using (AssetPack.Writer writer = AssetPack.Writer.Create(options.OutputPath, sparse: false, options.CreatedAt, options.CreatedBy))
        {
            Action? PrepareEntry(PackBuilderEntry entry)
            {
                string id = entry.Key.CanonicalId;

                return entry switch
                {
                    PackBuilderEntry.GameAsset asset => PrepareGameAsset(asset.Descriptor, id),
                    PackBuilderEntry.UnitSound sound => PrepareUnitSound(sound, id),
                    PackBuilderEntry.UnitAttack attack => PrepareUnitAttack(attack, id),
                    PackBuilderEntry.Background background => PrepareBackground(background, id),
                    PackBuilderEntry.BackgroundOverlay overlay => PrepareBackgroundOverlay(overlay, id),
                    _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
                };
            }

            Action? PrepareGameAsset(GameAssetDescriptor descriptor, string id)
            {
                return descriptor switch
                {
                    GameAssetDescriptor.Sprite spriteDescriptor => PrepareSprite(spriteDescriptor, id),
                    GameAssetDescriptor.Animation animationDescriptor => PrepareAnimation(animationDescriptor, id),
                    GameAssetDescriptor.Sound soundDescriptor => PrepareSound(soundDescriptor, id),
                    _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor, null)
                };
            }

            Action? PrepareUnitSound(PackBuilderEntry.UnitSound entry, string id)
            {
                AssetSound? sound = reader.Read(entry.Descriptor) ?? reader.Read(entry.RecordDescriptor);

                if (sound is null)
                {
                    return null;
                }

                AssetSound encoded = SoundEncoder.Encode(sound);

                return () => writer.AddSound(id, encoded);
            }

            Action? PrepareUnitAttack(PackBuilderEntry.UnitAttack entry, string id)
            {
                AssetAnimation? animation = reader.Read(entry.Descriptor);

                if (animation is null)
                {
                    return null;
                }

                AssetAnimation? recordAnimation = reader.Read(entry.RecordDescriptor);

                if (recordAnimation is null)
                {
                    return EncodeAnimation(animation, entry.Key, id);
                }
                else
                {
                    AssetAnimationMetadata metadata = animation.Metadata with
                    {
                        TriggerFrame = recordAnimation.Metadata.TriggerFrame,
                        SecondTriggerFrame = recordAnimation.Metadata.SecondTriggerFrame
                    };

                    return EncodeAnimation(animation with { Metadata = metadata }, entry.Key, id);
                }
            }

            Action? PrepareBackground(PackBuilderEntry.Background entry, string id)
            {
                UmbraBackgroundImage? image = UmbraBackgroundLibrary.TryGet(entry.BackgroundKey.CanonicalId);

                if (image is null)
                {
                    return null;
                }

                AssetPack.Writer.EncodedFrame frame = BackgroundBuilder.Build(image, entry.SceneWidth);

                return () => writer.AddSprite(id, frame, []);
            }

            Action? PrepareBackgroundOverlay(PackBuilderEntry.BackgroundOverlay entry, string id)
            {
                UmbraBackgroundImage? image = UmbraBackgroundLibrary.TryGet(entry.BackgroundKey.CanonicalId);

                if (image is not null)
                {
                    AssetAnimation? overlay = BackgroundOverlayBuilder.Build(entry.BackgroundKey, entry.Layer, image, BackgroundBuilder.GetMetadata(image, BackgroundBuilder.BattleSceneWidth));

                    if (overlay is not null)
                    {
                        return EncodeAnimation(overlay, entry.Key, id);
                    }
                }

                return null;
            }

            Action? PrepareSprite(GameAssetDescriptor.Sprite descriptor, string id)
            {
                AssetSprite? sprite = reader.Read(descriptor);

                if (sprite is null)
                {
                    return null;
                }

                FrameEncoder.EncodedEntry encoded = FrameEncoder.EncodeFrames([sprite], descriptor.Key, applied: [], EncodingType.None, source, quantizers);
                AssetPack.Writer.EncodedFrame frame = new(encoded.Metadata[0], encoded.Images[0], []);

                return () => writer.AddSprite(id, frame, encoded.Transforms);
            }

            Action? PrepareAnimation(GameAssetDescriptor.Animation descriptor, string id)
            {
                AssetAnimation? animation = reader.Read(descriptor);

                if (animation is null)
                {
                    return null;
                }

                return EncodeAnimation(animation, descriptor.Key, id);
            }

            Action? PrepareSound(GameAssetDescriptor.Sound descriptor, string id)
            {
                AssetSound? sound = reader.Read(descriptor);

                if (sound is null)
                {
                    return null;
                }

                AssetSound encoded = SoundEncoder.Encode(sound);

                return () => writer.AddSound(id, encoded);
            }

            Action EncodeAnimation(AssetAnimation animation, AssetKey key, string id)
            {
                IReadOnlyList<AssetSprite> sprites = animation.Frames;
                FrameEncoder.EncodedEntry encoded = FrameEncoder.EncodeFrames(sprites, key, applied: [], EncodingType.None, source, quantizers);

                List<AssetPack.Writer.EncodedFrame> frames = [];

                for (int index = 0; index < sprites.Count; index++)
                {
                    frames.Add(new AssetPack.Writer.EncodedFrame(encoded.Metadata[index], encoded.Images[index], []));
                }

                List<AssetSpriteMetadata> unfiltered = [];

                foreach (AssetSprite sprite in sprites)
                {
                    unfiltered.Add(sprite.Metadata);
                }

                AssetAnimationMetadata metadata = FrameEncoder.FitBounds(animation.Metadata, unfiltered, encoded.Metadata);

                return () => writer.AddAnimation(id, metadata, frames, encoded.Transforms);
            }

            Stopwatch progressTimer = Stopwatch.StartNew();
            ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            int processed = 0;

            foreach ((PackBuilderEntry entry, Action? writeEntry) in ParallelBatch.Prepare(
                entries, parallelOptions, _ => 1, MaxBatchEntries, PrepareEntry))
            {
                if (writeEntry is null)
                {
                    if (entry.Required)
                    {
                        missingRequired.Add(entry.Key.CanonicalId);
                    }
                }
                else
                {
                    writeEntry();
                }

                processed++;

                if (progressTimer.Elapsed >= ProgressInterval)
                {
                    progressTimer.Restart();
                    reportProgress(new PackBuilderProgress(processed, entries.Count));
                }
            }

            reportProgress(new PackBuilderProgress(entries.Count, entries.Count));

            if (missingRequired.Count > 0)
            {
                return new PackBuilderResult.MissingRequired(options.OutputPath, missingRequired);
            }

            writer.Complete();
        }

        return new PackBuilderResult.Packed(options.OutputPath);
    }
}
