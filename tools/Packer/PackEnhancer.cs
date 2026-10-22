using System.Diagnostics;

namespace Packer;

internal sealed partial class PackEnhancer(AssetPack pack, Action<PackEnhancerProgress> reportProgress)
{
    private const long MaxBatchPixels = 64L * 1024 * 1024;

    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(5);

    private static readonly IReadOnlyList<AssetSpriteLayerType> SurfaceLayers = GetSurfaceLayers();

    public PackEnhancerResult Enhance(PackEnhancerOptions options)
    {
        List<SelectedEntry> matched = SelectEntries(options.Categories, options.Enhancements);

        if (options.Categories is not null)
        {
            IReadOnlyList<string> unmatched = GetUnmatchedPatterns(options.Categories, matched);

            if (unmatched.Count > 0)
            {
                return new PackEnhancerResult.UnmatchedCategories(unmatched);
            }
        }

        List<SelectedEntry> selected = ExpandSurfaceGroups(matched, options.Enhancements);

        if (selected.Count > 0)
        {
            reportProgress(GetSelectionProgress(selected));

            Directory.CreateDirectory(options.CacheDirectory);

            List<HashedEntry> entries = HashFrames(selected);
            EnhancementCache cache = new(options.CacheDirectory, options.Device);

            int upscaleCount = ComputeUpscale(entries, cache, options);
            int refineCount = ComputeRefine(entries, cache, options);
            int alphaCount = ComputeAlpha(entries, cache, options);
            int surfaceCount = ComputeSurface(entries, cache, options);

            if (options.OutputPath is not null)
            {
                WritePack(options.OutputPath, options.Sparse, options.CreatedAt, options.CreatedBy, entries, cache);
            }

            return new PackEnhancerResult.Enhanced(
                options.OutputPath,
                entries.Count,
                upscaleCount + refineCount + alphaCount + surfaceCount);
        }
        else
        {
            return new PackEnhancerResult.NoSelection();
        }
    }

    private static PackEnhancerProgress.Selected GetSelectionProgress(List<SelectedEntry> selected)
    {
        int upscaleCount = 0;
        int refineCount = 0;
        int alphaCount = 0;
        int surfaceCount = 0;

        foreach (SelectedEntry entry in selected)
        {
            if (entry.Enhancements.Contains(EnhancementType.Upscale))
            {
                upscaleCount++;
            }

            if (entry.Enhancements.Contains(EnhancementType.Refine))
            {
                refineCount++;
            }

            if (entry.Enhancements.Contains(EnhancementType.Alpha))
            {
                alphaCount++;
            }

            if (entry.Enhancements.Contains(EnhancementType.Surface))
            {
                surfaceCount++;
            }
        }

        return new PackEnhancerProgress.Selected(
            selected.Count, upscaleCount, refineCount, alphaCount, surfaceCount);
    }

    private static IReadOnlyList<string> GetUnmatchedPatterns(CategorySelector categories, List<SelectedEntry> selected)
    {
        List<string> selectedIds = [];

        foreach (SelectedEntry entry in selected)
        {
            selectedIds.Add(entry.Entry.Id);
        }

        return categories.UnmatchedPatterns(selectedIds);
    }

    private static List<AssetSpriteLayerType> GetSurfaceLayers()
    {
        List<AssetSpriteLayerType> layers = [];

        foreach (IReadOnlyList<UmbraSurfaceLayerType> group in UmbraSurfaceModel.LayerGroups)
        {
            foreach (UmbraSurfaceLayerType type in group)
            {
                layers.Add(type.FromUmbra());
            }
        }

        return layers;
    }

    private static long GetSourcePixels(PackImageEntry entry)
    {
        long pixels = 0;

        foreach (AssetSpriteMetadata metadata in entry.FrameMetadata)
        {
            pixels += (long)metadata.PixelWidth * metadata.PixelHeight;
        }

        return pixels;
    }

    private static bool HasAllSpriteLayers(PackImageEntry entry)
    {
        foreach (IReadOnlyList<AssetManifestSpriteLayer> frame in entry.FrameLayers)
        {
            HashSet<AssetSpriteLayerType> present = [];

            foreach (AssetManifestSpriteLayer layer in frame)
            {
                present.Add(layer.Type);
            }

            foreach (AssetSpriteLayerType type in SurfaceLayers)
            {
                if (!present.Contains(type))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static FrameEncoder.EncodedEntry EncodeEnhanced(
        HashedEntry entry,
        EnhancementCache cache,
        ISpriteFilterSource source,
        FrameEncoder.QuantizerPool quantizers)
    {
        List<AssetTransform> history = [.. entry.Entry.Transforms];

        if (entry.Enhancements.Contains(EnhancementType.Upscale))
        {
            UpscalerType upscaler = UpscaleMap.GetUpscaler(entry.Entry.Key);

            history.Add(new AssetTransform(
                UpscaleMap.GetTransform(upscaler), GetInputScale(entry, cache, EnhancementType.Upscale)));
        }

        if (entry.Enhancements.Contains(EnhancementType.Refine))
        {
            history.Add(new AssetTransform(EnhancementMap.Refine, GetInputScale(entry, cache, EnhancementType.Refine)));
        }

        if (entry.Enhancements.Contains(EnhancementType.Alpha))
        {
            history.Add(new AssetTransform(EnhancementMap.Alpha, GetInputScale(entry, cache, EnhancementType.Alpha)));
        }

        List<AssetSprite> frames = ResolveEnhancedSprites(entry, cache);
        EncodingType encodingType = EncodingMap.GetEncoding(entry.Entry.Key);

        return FrameEncoder.EncodeFrames(frames, entry.Entry.Key, history, encodingType, source, quantizers);
    }

    private static int GetInputScale(HashedEntry entry, EnhancementCache cache, EnhancementType enhancement)
    {
        AssetSpriteMetadata metadata = entry.Entry.FrameMetadata[0];
        IReadOnlyList<EnhancementType> inputEnhancements = EnhancementMap.GetInputEnhancements(entry.Enhancements, enhancement);

        if (inputEnhancements.Count > 0)
        {
            AssetSprite sprite = ResolveSprite(cache, inputEnhancements, entry.Upscaler, entry.FrameHashes[0], metadata, GetPromptHash(entry, inputEnhancements));

            return sprite.Metadata.PixelScale;
        }
        else
        {
            return metadata.PixelScale;
        }
    }

    private static List<AssetSprite> ResolveEnhancedSprites(HashedEntry entry, EnhancementCache cache)
    {
        if (entry.Enhancements.Contains(EnhancementType.Surface))
        {
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetInputEnhancements(entry.Enhancements, EnhancementType.Surface);

            return ResolveSprites(entry, enhancements, cache);
        }
        else
        {
            return ResolveSprites(entry, entry.Enhancements, cache);
        }
    }

    private List<AssetSprite> ResolveInputSprites(HashedEntry entry, EnhancementType enhancement, EnhancementCache cache)
    {
        IReadOnlyList<EnhancementType> inputEnhancements = EnhancementMap.GetInputEnhancements(entry.Enhancements, enhancement);

        if (inputEnhancements.Count > 0)
        {
            return ResolveSprites(entry, inputEnhancements, cache);
        }
        else
        {
            return [.. entry.Entry.ReadFrames(pack)];
        }
    }

    private static UmbraPromptHash? GetPromptHash(HashedEntry entry, IReadOnlyList<EnhancementType> enhancements)
    {
        if (enhancements.Contains(EnhancementType.Refine))
        {
            return entry.PromptHash;
        }
        else
        {
            return null;
        }
    }

    private static List<AssetSprite> ResolveSprites(
        HashedEntry entry,
        IReadOnlyList<EnhancementType> enhancements,
        EnhancementCache cache)
    {
        IReadOnlyList<AssetSpriteMetadata> metadata = entry.Entry.FrameMetadata;
        Dictionary<UmbraSpriteHash, AssetSprite> resolved = [];
        List<AssetSprite> frames = [];

        for (int index = 0; index < metadata.Count; index++)
        {
            UmbraSpriteHash hash = entry.FrameHashes[index];

            if (!resolved.TryGetValue(hash, out AssetSprite? sprite))
            {
                sprite = ResolveSprite(cache, enhancements, entry.Upscaler, hash, metadata[index], GetPromptHash(entry, enhancements));
                resolved[hash] = sprite;
            }

            frames.Add(sprite);
        }

        return frames;
    }

    private static AssetSprite ResolveSprite(
        EnhancementCache cache,
        IReadOnlyList<EnhancementType> enhancements,
        UpscalerType? upscaler,
        UmbraSpriteHash hash,
        AssetSpriteMetadata metadata,
        UmbraPromptHash? promptHash)
    {
        EnhancementCache.Image image = cache.Read(new EnhancementCache.Key(enhancements, upscaler, hash, null, promptHash));

        return new AssetSprite(metadata with { PixelWidth = image.PixelWidth, PixelHeight = image.PixelHeight }, image.Pixels);
    }

    private static AssetSpriteLayer ResolveLayer(EnhancementCache cache, EnhancementCache.Key key, AssetSpriteMetadata metadata)
    {
        EnhancementCache.Image image = cache.Read(key);

        if (image.PixelWidth != metadata.PixelWidth || image.PixelHeight != metadata.PixelHeight)
        {
            throw new InvalidOperationException($"Unexpected '{key.LayerType}' layer size {image.PixelWidth}x{image.PixelHeight} for frame '{key.Hash}'.");
        }

        return new AssetSpriteLayer(image.PixelWidth, image.PixelHeight, image.Pixels);
    }

    private static List<HashedEntry> FilterByEnhancement(List<HashedEntry> entries, EnhancementType enhancement)
    {
        List<HashedEntry> result = [];

        foreach (HashedEntry entry in entries)
        {
            if (entry.Enhancements.Contains(enhancement))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static List<HashedEntry> GetMissingEntries(
        List<HashedEntry> entries,
        EnhancementPass pass,
        EnhancementCache cache)
    {
        List<HashedEntry> result = [];

        foreach (HashedEntry entry in FilterByEnhancement(entries, pass.Enhancement))
        {
            List<EnhancementCache.Key> keys = pass.GetOutputKeys(entry);

            if (!cache.ContainsAll(keys))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static IReadOnlyList<EnhancementType> SelectEnhancements(
        PackImageEntry entry,
        IReadOnlyList<EnhancementType> requested)
    {
        IReadOnlyList<EnhancementType> supported = EnhancementMap.GetEnhancements(entry.Key);
        bool hasAllSpriteLayers = HasAllSpriteLayers(entry);
        List<EnhancementType> selected = [];

        foreach (EnhancementType enhancement in supported)
        {
            bool alreadyApplied = enhancement == EnhancementType.Surface && hasAllSpriteLayers;

            if (!alreadyApplied && IsRequested(requested, enhancement))
            {
                selected.Add(enhancement);
            }
        }

        return selected;
    }

    private static bool IsRequested(IReadOnlyList<EnhancementType> requested, EnhancementType enhancement)
    {
        foreach (EnhancementType candidate in requested)
        {
            if (enhancement <= candidate)
            {
                return true;
            }
        }

        return false;
    }

    private List<SelectedEntry> ExpandSurfaceGroups(List<SelectedEntry> matched, IReadOnlyList<EnhancementType> requested)
    {
        List<PackImageEntry> entries = [.. PackImageEntry.Enumerate(pack)];
        Dictionary<string, IReadOnlyList<EnhancementType>> supported = [];
        List<string> candidateIds = [];

        foreach (PackImageEntry entry in entries)
        {
            IReadOnlyList<EnhancementType> enhancements = SelectEnhancements(entry, requested);

            supported[entry.Id] = enhancements;

            if (enhancements.Contains(EnhancementType.Surface))
            {
                candidateIds.Add(entry.Id);
            }
        }

        Dictionary<string, IReadOnlyList<EnhancementType>> selectedById = [];
        List<string> surfaceIds = [];

        foreach (SelectedEntry entry in matched)
        {
            selectedById[entry.Entry.Id] = entry.Enhancements;

            if (entry.Enhancements.Contains(EnhancementType.Surface))
            {
                surfaceIds.Add(entry.Entry.Id);
            }
        }

        foreach (string id in UnitSequenceGroupBuilder.ExpandGroups(surfaceIds, candidateIds))
        {
            selectedById.TryAdd(id, supported[id]);
        }

        List<SelectedEntry> result = [];

        foreach (PackImageEntry entry in entries)
        {
            if (selectedById.TryGetValue(entry.Id, out IReadOnlyList<EnhancementType>? enhancements))
            {
                result.Add(new SelectedEntry(entry, enhancements));
            }
        }

        return result;
    }

    private List<SelectedEntry> SelectEntries(CategorySelector? categories, IReadOnlyList<EnhancementType> requested)
    {
        List<PackImageEntry> targets = SelectTargets(categories);
        List<SelectedEntry> selected = [];

        foreach (PackImageEntry entry in targets)
        {
            IReadOnlyList<EnhancementType> enhancements = SelectEnhancements(entry, requested);

            if (enhancements.Count > 0)
            {
                selected.Add(new SelectedEntry(entry, enhancements));
            }
        }

        return selected;
    }

    private List<PackImageEntry> SelectTargets(CategorySelector? categories)
    {
        List<PackImageEntry> targets = [];

        foreach (PackImageEntry entry in PackImageEntry.Enumerate(pack))
        {
            if (categories is null || categories.Matches(entry.Id))
            {
                targets.Add(entry);
            }
        }

        return targets;
    }

    private List<HashedEntry> HashFrames(List<SelectedEntry> selected)
    {
        List<HashedEntry> entries = [];

        foreach (SelectedEntry entry in selected)
        {
            List<UmbraSpriteHash> hashes = [];

            foreach (AssetSprite frame in entry.Entry.ReadFrames(pack))
            {
                UmbraSpriteHash hash = frame.ToHash();

                hashes.Add(hash);
            }

            UpscalerType? upscaler = null;

            if (entry.Enhancements.Contains(EnhancementType.Upscale))
            {
                upscaler = UpscaleMap.GetUpscaler(entry.Entry.Key);
            }

            entries.Add(new HashedEntry(entry.Entry, hashes, entry.Enhancements, upscaler, RefinePromptHash.Get(entry.Entry.Key)));
        }

        return entries;
    }

    private List<UmbraSequenceGroup> BuildEntryGroups(List<HashedEntry> entries)
    {
        List<UmbraSequenceGroup> groups = [];

        foreach (HashedEntry entry in entries)
        {
            UmbraSequenceId sequenceId = new(entry.Entry.Id);

            groups.Add(new UmbraSequenceGroup(sequenceId, [new UmbraSequence(sequenceId, BuildSequenceFrames(entry))]));
        }

        return groups;
    }

    private List<UmbraFrame> BuildSequenceFrames(HashedEntry entry)
    {
        IReadOnlyList<AssetSprite> frames = entry.Entry.ReadFrames(pack);
        List<UmbraFrame> sequenceFrames = [];

        for (int index = 0; index < frames.Count; index++)
        {
            UmbraSpriteHash hash = entry.FrameHashes[index];
            UmbraSprite sprite = frames[index].ToUmbra();

            sequenceFrames.Add(new UmbraFrame(hash, sprite));
        }

        return sequenceFrames;
    }

    private static List<EnhancementOutput> GetOutputs(List<HashedEntry> entries, EnhancementType enhancement)
    {
        List<EnhancementOutput> outputs = [];

        foreach (HashedEntry entry in entries)
        {
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetOutputEnhancements(entry.Enhancements, enhancement);
            UmbraPromptHash? promptHash = GetPromptHash(entry, enhancements);

            if (!outputs.Exists(output => output.Matches(enhancements, entry.Upscaler, promptHash)))
            {
                outputs.Add(new EnhancementOutput(enhancements, entry.Upscaler, promptHash));
            }
        }

        return outputs;
    }

    private static List<HashedEntry> FilterByOutput(
        List<HashedEntry> entries,
        EnhancementType enhancement,
        EnhancementOutput output)
    {
        List<HashedEntry> result = [];

        foreach (HashedEntry entry in entries)
        {
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetOutputEnhancements(entry.Enhancements, enhancement);

            if (output.Matches(enhancements, entry.Upscaler, GetPromptHash(entry, enhancements)))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static List<HashedEntry> FilterByUpscaler(List<HashedEntry> entries, UpscalerType? upscaler)
    {
        List<HashedEntry> result = [];

        foreach (HashedEntry entry in entries)
        {
            if (entry.Upscaler == upscaler)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private int ComputeUpscale(List<HashedEntry> entries, EnhancementCache cache, PackEnhancerOptions options)
    {
        List<HashedEntry> missing = GetMissingEntries(entries, EnhancementPass.Upscale, cache);
        List<HashedEntry> vsr = FilterByUpscaler(missing, UpscalerType.Vsr);
        List<HashedEntry> mosr = FilterByUpscaler(missing, UpscalerType.Mosr);
        int total = 0;

        if (vsr.Count > 0)
        {
            using UmbraVsrUpscaleModel model = new(options.ModelDirectory, options.ScratchDirectory, options.Device);

            foreach (UmbraUpscaledFrame frame in model.Run(BuildEntryGroups(vsr)))
            {
                WriteSprite(cache, [EnhancementType.Upscale], UpscalerType.Vsr, frame, null);

                total++;
            }
        }

        if (mosr.Count > 0)
        {
            using UmbraMosrUpscaleModel model = new(options.ModelDirectory, options.Device);

            foreach (UmbraUpscaledFrame frame in model.Run(BuildEntrySequences(mosr)))
            {
                WriteSprite(cache, [EnhancementType.Upscale], UpscalerType.Mosr, frame, null);

                total++;
            }
        }

        return total;
    }

    private List<UmbraSequence> BuildEntrySequences(List<HashedEntry> entries)
    {
        List<UmbraSequence> sequences = [];

        foreach (HashedEntry entry in entries)
        {
            UmbraSequenceId sequenceId = new(entry.Entry.Id);

            sequences.Add(new UmbraSequence(sequenceId, BuildSequenceFrames(entry)));
        }

        return sequences;
    }

    private int ComputeRefine(List<HashedEntry> entries, EnhancementCache cache, PackEnhancerOptions options)
    {
        int total = 0;

        List<HashedEntry> candidates = FilterByEnhancement(entries, EnhancementType.Refine);
        List<HashedEntry> missing = GetMissingEntries(entries, EnhancementPass.Refine, cache);

        if (missing.Count > 0)
        {
            RefinePromptConfiguration promptConfiguration = BuildRefinePromptConfiguration(candidates);
            RefineRepaintConfiguration repaintConfiguration = BuildRefineRepaintConfiguration(candidates);
            IReadOnlyDictionary<UmbraSequenceId, RefineConfiguration> configurations = BuildRefineConfigurations(candidates);
            using UmbraRefineModel model = new(options.ModelDirectory, options.Device, promptConfiguration, repaintConfiguration, configurations);

            foreach (EnhancementOutput output in GetOutputs(missing, EnhancementType.Refine))
            {
                List<HashedEntry> outputMissing = FilterByOutput(missing, EnhancementType.Refine, output);
                List<HashedEntry> outputCandidates = FilterByOutput(candidates, EnhancementType.Refine, output);
                IEnumerable<UmbraSequenceGroup> groups = BuildUnitGroups(
                    outputMissing, outputCandidates, entry => ResolveInputSprites(entry, EnhancementType.Refine, cache));

                foreach (UmbraUpscaledFrame frame in model.Run(groups))
                {
                    WriteSprite(cache, output.Enhancements, output.Upscaler, frame, output.PromptHash);
                    total++;
                }
            }
        }

        return total;
    }

    private int ComputeAlpha(List<HashedEntry> entries, EnhancementCache cache, PackEnhancerOptions options)
    {
        int total = 0;

        List<HashedEntry> candidates = FilterByEnhancement(entries, EnhancementType.Alpha);
        List<HashedEntry> missing = GetMissingEntries(entries, EnhancementPass.Alpha, cache);

        if (missing.Count > 0)
        {
            using UmbraAlphaModel model = new(options.ModelDirectory, options.Device);

            foreach (EnhancementOutput output in GetOutputs(missing, EnhancementType.Alpha))
            {
                List<HashedEntry> outputMissing = FilterByOutput(missing, EnhancementType.Alpha, output);
                List<HashedEntry> outputCandidates = FilterByOutput(candidates, EnhancementType.Alpha, output);
                IEnumerable<UmbraSequenceGroup> groups = BuildUnitGroups(
                    outputMissing, outputCandidates, entry => ResolveInputSprites(entry, EnhancementType.Alpha, cache));

                foreach (UmbraUpscaledFrame frame in model.Run(groups, new UmbraDataProvider(cache, output.Upscaler)))
                {
                    WriteSprite(cache, output.Enhancements, output.Upscaler, frame, output.PromptHash);
                    total++;
                }
            }
        }

        return total;
    }

    private int ComputeSurface(List<HashedEntry> entries, EnhancementCache cache, PackEnhancerOptions options)
    {
        int total = 0;

        List<HashedEntry> candidates = FilterByEnhancement(entries, EnhancementType.Surface);

        foreach (IReadOnlyList<UmbraSurfaceLayerType> layers in UmbraSurfaceModel.LayerGroups)
        {
            List<HashedEntry> missing = GetMissingEntries(entries, EnhancementPass.Surface(layers), cache);

            if (missing.Count > 0)
            {
                using UmbraSurfaceModel model = new(layers, options.ModelDirectory, options.SurfaceDenoisingSteps, options.Device);

                foreach (EnhancementOutput output in GetOutputs(missing, EnhancementType.Surface))
                {
                    List<HashedEntry> outputMissing = FilterByOutput(missing, EnhancementType.Surface, output);
                    List<HashedEntry> outputCandidates = FilterByOutput(candidates, EnhancementType.Surface, output);
                    IEnumerable<UmbraSequenceGroup> groups = BuildUnitGroups(
                        outputMissing, outputCandidates, entry => ResolveInputSprites(entry, EnhancementType.Surface, cache));

                    foreach (UmbraSurfaceLayer layer in model.Run(groups))
                    {
                        AssetSpriteLayerType layerType = layer.Type.FromUmbra();
                        UmbraSpriteHash hash = layer.Hash;

                        cache.Write(
                            new EnhancementCache.Key(output.Enhancements, output.Upscaler, hash, layerType, output.PromptHash),
                            layer.PixelWidth,
                            layer.PixelHeight,
                            layer.Pixels);

                        total++;
                    }
                }
            }
        }

        return total;
    }

    private static void WriteSprite(
        EnhancementCache cache,
        IReadOnlyList<EnhancementType> enhancements,
        UpscalerType? upscaler,
        UmbraUpscaledFrame frame,
        UmbraPromptHash? promptHash)
    {
        UmbraSpriteMetadata metadata = frame.Sprite.Metadata;
        UmbraSpriteHash hash = frame.Hash;
        EnhancementCache.Key key = new(enhancements, upscaler, hash, null, promptHash);

        cache.Write(key, metadata.PixelWidth, metadata.PixelHeight, frame.Sprite.Pixels);
    }

    private static RefinePromptConfiguration BuildRefinePromptConfiguration(List<HashedEntry> entries)
    {
        Dictionary<UmbraSequenceId, RefinePrompt> prompts = [];

        foreach (HashedEntry entry in entries)
        {
            RefinePrompt? prompt = RefinePromptMap.GetPrompt(entry.Entry.Key);

            if (prompt is not null)
            {
                UmbraSequenceId sequenceId = new(entry.Entry.Id);

                prompts[sequenceId] = prompt;
            }
        }

        return new RefinePromptConfiguration(prompts);
    }

    private static RefineRepaintConfiguration BuildRefineRepaintConfiguration(List<HashedEntry> entries)
    {
        Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>> regions = [];

        foreach (HashedEntry entry in entries)
        {
            IReadOnlyList<RefineRegion> entryRegions = RefineRepaintMap.GetRegions(entry.Entry.Key);

            if (entryRegions.Count > 0)
            {
                UmbraSequenceId sequenceId = new(entry.Entry.Id);

                regions[sequenceId] = entryRegions;
            }
        }

        return new RefineRepaintConfiguration(regions);
    }

    private static Dictionary<UmbraSequenceId, RefineConfiguration> BuildRefineConfigurations(List<HashedEntry> entries)
    {
        Dictionary<UmbraSequenceId, RefineConfiguration> configurations = [];

        foreach (HashedEntry entry in entries)
        {
            UmbraSequenceId sequenceId = new(entry.Entry.Id);

            configurations[sequenceId] = RefineConfigurationMap.GetConfiguration(entry.Entry.Key);
        }

        return configurations;
    }

    private static IEnumerable<UmbraSequenceGroup> BuildUnitGroups(
        List<HashedEntry> missing,
        List<HashedEntry> candidates,
        Func<HashedEntry, List<AssetSprite>> resolveSprites)
    {
        Dictionary<string, HashedEntry> candidatesById = [];

        foreach (HashedEntry entry in candidates)
        {
            candidatesById[entry.Entry.Id] = entry;
        }

        List<string> missingIds = [];

        foreach (HashedEntry entry in missing)
        {
            missingIds.Add(entry.Entry.Id);
        }

        return UnitSequenceGroupBuilder.Build(missingIds, candidatesById.Keys, LoadFrames);

        List<UmbraFrame> LoadFrames(string id)
        {
            HashedEntry entry = candidatesById[id];

            return BuildFrames(entry, resolveSprites(entry));
        }
    }

    private static List<UmbraFrame> BuildFrames(HashedEntry entry, IReadOnlyList<AssetSprite> frames)
    {
        Debug.Assert(entry.Enhancements.Contains(EnhancementType.Upscale));

        List<UmbraFrame> sequenceFrames = [];

        for (int index = 0; index < frames.Count; index++)
        {
            UmbraSpriteHash hash = entry.FrameHashes[index];

            sequenceFrames.Add(new UmbraFrame(hash, frames[index].ToUmbra()));
        }

        return sequenceFrames;
    }

    private void WritePack(string outputPath, bool sparse, DateTimeOffset createdAt, string createdBy, List<HashedEntry> entries, EnhancementCache cache)
    {
        Dictionary<string, HashedEntry> selected = [];

        foreach (HashedEntry entry in entries)
        {
            selected[entry.Entry.Id] = entry;
        }

        using AssetPack.Writer writer = AssetPack.Writer.Create(outputPath, sparse, createdAt, createdBy);
        FrameEncoder.QuantizerPool quantizers = new();
        PackFilterSource source = new(pack);
        List<PackEntry> packEntries = [];

        foreach (PackEntry entry in PackEntry.EnumerateAll(pack))
        {
            if (!sparse || selected.ContainsKey(entry.Id))
            {
                packEntries.Add(entry);
            }
        }

        int written = 0;

        long GetCost(PackEntry entry)
        {
            if (selected.TryGetValue(entry.Id, out HashedEntry? hashed))
            {
                return GetSourcePixels(hashed.Entry);
            }
            else
            {
                return 0;
            }
        }

        PreparedEntry? Prepare(PackEntry entry)
        {
            if (selected.TryGetValue(entry.Id, out HashedEntry? hashed))
            {
                return PrepareEntry(hashed, cache, source, quantizers);
            }
            else
            {
                return null;
            }
        }

        Stopwatch progressTimer = Stopwatch.StartNew();
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

        foreach ((PackEntry entry, PreparedEntry? prepared) in ParallelBatch.Prepare(
            packEntries, parallelOptions, GetCost, MaxBatchPixels, Prepare))
        {
            if (prepared is not null)
            {
                prepared.Entry.Write(writer, prepared.Frames, prepared.Transforms);

                written++;

                if (progressTimer.Elapsed >= ProgressInterval)
                {
                    progressTimer.Restart();
                    reportProgress(new PackEnhancerProgress.Written(written, entries.Count));
                }
            }
            else
            {
                entry.CopyTo(pack, writer);
            }
        }

        reportProgress(new PackEnhancerProgress.Written(written, entries.Count));

        writer.Complete();
    }

    private PreparedEntry PrepareEntry(
        HashedEntry entry,
        EnhancementCache cache,
        ISpriteFilterSource source,
        FrameEncoder.QuantizerPool quantizers)
    {
        IReadOnlyList<AssetSpriteMetadata> metadata;
        IReadOnlyList<AssetPack.Writer.EncodedImage> images;
        IReadOnlyList<AssetTransform> transforms;

        if (entry.Enhancements.Contains(EnhancementType.Upscale) ||
            entry.Enhancements.Contains(EnhancementType.Refine))
        {
            FrameEncoder.EncodedEntry encoded = EncodeEnhanced(entry, cache, source, quantizers);

            metadata = encoded.Metadata;
            images = encoded.Images;
            transforms = encoded.Transforms;
        }
        else
        {
            metadata = entry.Entry.FrameMetadata;
            images = entry.Entry.ReadEncodedFrames(pack);
            transforms = entry.Entry.Transforms;
        }

        IReadOnlyList<IReadOnlyList<AssetPack.Writer.EncodedSpriteLayer>> frameLayers = ResolveFrameLayers(entry, metadata, cache);
        List<AssetPack.Writer.EncodedFrame> frames = [];

        for (int index = 0; index < metadata.Count; index++)
        {
            frames.Add(new AssetPack.Writer.EncodedFrame(metadata[index], images[index], frameLayers[index]));
        }

        return new PreparedEntry(entry.Entry, frames, transforms);
    }

    private IReadOnlyList<IReadOnlyList<AssetPack.Writer.EncodedSpriteLayer>> ResolveFrameLayers(
        HashedEntry entry,
        IReadOnlyList<AssetSpriteMetadata> metadata,
        EnhancementCache cache)
    {
        IReadOnlyList<IReadOnlyList<AssetManifestSpriteLayer>> existing = entry.Entry.FrameLayers;
        List<IReadOnlyList<AssetPack.Writer.EncodedSpriteLayer>> frames = [];

        if (entry.Enhancements.Contains(EnhancementType.Surface))
        {
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetOutputEnhancements(entry.Enhancements, EnhancementType.Surface);
            Dictionary<EnhancementCache.Key, AssetPack.Writer.EncodedSpriteLayer> encoded = [];

            for (int index = 0; index < existing.Count; index++)
            {
                List<AssetPack.Writer.EncodedSpriteLayer> frame = [];

                foreach (AssetSpriteLayerType type in SurfaceLayers)
                {
                    EnhancementCache.Key key = new(enhancements, entry.Upscaler, entry.FrameHashes[index], type, GetPromptHash(entry, enhancements));

                    if (!encoded.TryGetValue(key, out AssetPack.Writer.EncodedSpriteLayer? layer))
                    {
                        AssetSpriteLayer resolved = ResolveLayer(cache, key, metadata[index]);
                        AssetPack.Writer.EncodedImage image = FrameEncoder.EncodeSurfaceLayer(resolved, type);

                        layer = new AssetPack.Writer.EncodedSpriteLayer(type, image);
                        encoded[key] = layer;
                    }

                    frame.Add(layer);
                }

                foreach (AssetManifestSpriteLayer layer in existing[index])
                {
                    if (!SurfaceLayers.Contains(layer.Type))
                    {
                        AssetPack.Writer.EncodedImage image = new(layer.PixelWidth, layer.PixelHeight, pack.ReadSpriteLayer(layer));

                        frame.Add(new AssetPack.Writer.EncodedSpriteLayer(layer.Type, image));
                    }
                }

                frames.Add(frame);
            }
        }
        else
        {
            foreach (IReadOnlyList<AssetManifestSpriteLayer> frame in existing)
            {
                frames.Add(PackImageEntry.ReadSpriteLayers(pack, frame));
            }
        }

        return frames;
    }
}
