using TorchSharp;

namespace Umbra.Ltx;

internal sealed class Refiner(
    LtxDit transformer,
    LtxVae vae,
    LtxLatentUpsampler upsampler,
    RefineContext contexts,
    RefineRepaintConfiguration repaintConfiguration,
    IReadOnlyDictionary<UmbraSequenceId, RefineConfiguration> configurations,
    RefineOptions options) : IDisposable
{
    private const int ReferenceScale = 2;
    private const double MinScaledAlpha = 1.0 / 255.0;
    private const double MinimumLight = 1.0 / 255.0;
    private const double LoraStrength = 0.5;

    public static Refiner Load(
        string directory,
        torch.Device device,
        torch.ScalarType dtype,
        RefinePromptConfiguration promptConfiguration,
        RefineRepaintConfiguration repaintConfiguration,
        IReadOnlyDictionary<UmbraSequenceId, RefineConfiguration> configurations)
    {
        RefineOptions options = new(device, dtype, dtype, LtxSchedule.Detailing);

        LtxDit transformer = MetaDevice.Construct(() => new LtxDit(LtxDitOptions.Distilled));
        IReadOnlySet<string> parameterNames = ModuleParameters.ListParameterNames(transformer);
        Dictionary<string, torch.Tensor> transformerState = RefineWeights.MapTransformer(directory, parameterNames);

        ModuleParameters.Load(transformer, transformerState, torch.CPU);

        transformer.MoveResidentTo(dtype, device);
        transformer.StreamBlocks(
            RefineWeights.MapLora(directory, LoraStrength, parameterNames),
            options.CachedBlocks,
            dtype,
            device);

        LtxVae vae = MetaDevice.Construct(() => new LtxVae(LtxVaeOptions.Distilled));
        ModuleParameters.Load(vae, RefineWeights.Map(directory, RefineWeights.VaeFileName, dtype), device);

        LtxLatentUpsampler upsampler = MetaDevice.Construct(() => new LtxLatentUpsampler(128, 1024, 4));
        ModuleParameters.Load(upsampler, RefineWeights.Map(directory, RefineWeights.UpsamplerFileName, dtype), device);

        RefineContext context = RefineContext.Load(directory, promptConfiguration);

        return new Refiner(transformer, vae, upsampler, context, repaintConfiguration, configurations, options);
    }

    public IEnumerable<UmbraUpscaledFrame> Run(IEnumerable<UmbraSequenceGroup> groups)
    {
        HashSet<UmbraSpriteHash> frameHashes = [];

        foreach (RefineClip clip in RefineClipBuilder.Build(groups))
        {
            foreach (UmbraUpscaledFrame frame in RunClip(clip))
            {
                if (frameHashes.Add(frame.Hash))
                {
                    yield return frame;
                }
            }
        }
    }

    public void Dispose()
    {
        transformer.Dispose();
        vae.Dispose();
        upsampler.Dispose();
        contexts.Dispose();
    }

    private List<UmbraUpscaledFrame> RunClip(RefineClip clip)
    {
        using IDisposable scope = torch.NewDisposeScope();

        ChromaKey chromaKey = ChromaKey.Select(clip.Frames[0].Frame.Sprite);
        torch.Tensor canvas = RefineCanvas.Build(clip, torch.CPU);
        torch.Tensor additiveMask = BuildAdditiveMask(clip, canvas.device);
        torch.Tensor frames = Restore(clip, canvas, additiveMask, chromaKey);

        List<UmbraUpscaledFrame> results = [];

        for (int position = 0; position < clip.Frames.Count; position++)
        {
            RefineClipFrame clipFrame = clip.Frames[position];

            using torch.Tensor decodedFrame = frames.select(0, position);
            using torch.Tensor frameColors = decodedFrame.to(torch.ScalarType.Float32);

            UmbraSprite frame = CutFrame(frameColors, canvas, clipFrame, position, chromaKey);

            results.Add(new UmbraUpscaledFrame(clipFrame.Frame.Hash, frame));
        }

        return results;
    }

    private static torch.Tensor BuildAdditiveMask(RefineClip clip, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        float[] flags = new float[clip.Frames.Count];

        for (int position = 0; position < clip.Frames.Count; position++)
        {
            if (clip.Frames[position].Frame.Sprite.Metadata.Additive)
            {
                flags[position] = 1;
            }
        }

        using torch.Tensor mask = torch.tensor(flags, torch.ScalarType.Float32, device);

        return mask.view(clip.Frames.Count, 1, 1, 1).contiguous().MoveToOuterDisposeScope();
    }

    private static torch.Tensor PaintRegions(RefineClip clip, IReadOnlyList<RefineRegion> regions, long canvasScale, long height, long width)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor plane = torch.zeros([1, 1, height, width], torch.ScalarType.Float32);

        for (int index = 0; index < regions.Count; index++)
        {
            RefineRegion region = regions[index];

            foreach (RefineClipFrame clipFrame in clip.Frames)
            {
                long scale = clipFrame.Frame.Sprite.Metadata.PixelScale * canvasScale;

                plane.select(0, 0)
                    .narrow(1, (clipFrame.Top * canvasScale) + (region.Y * scale), region.Height * scale)
                    .narrow(2, (clipFrame.Left * canvasScale) + (region.X * scale), region.Width * scale)
                    .fill_(index + 1);
            }
        }

        return plane.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Compose(torch.Tensor canvas, torch.Tensor additiveMask, ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor keyPlanes = key.ToPlanes(canvas.device);
        using torch.Tensor colors = canvas.narrow(1, 0, 3);
        using torch.Tensor alpha = canvas.narrow(1, 3, 1);
        using torch.Tensor transparency = alpha.neg().add(1.0);
        using torch.Tensor composite = colors.mul(alpha).add(transparency.mul(keyPlanes));
        using torch.Tensor additiveFrames = additiveMask.gt(0.0);

        return torch.where(additiveFrames, colors, composite).MoveToOuterDisposeScope();
    }

    private static long RoundUp(long value, long multiple)
    {
        return (value + multiple - 1) / multiple * multiple;
    }

    private static int GetCyclesFrameCount(IEnumerable<RefineCycle> cycles)
    {
        int count = 0;

        foreach (RefineCycle cycle in cycles)
        {
            count += cycle.Length;
        }

        return count;
    }

    private IReadOnlyList<RefineCycle> LayoutCycles(torch.Tensor canvas, IReadOnlyList<int> cycleLengths)
    {
        List<RefineCycle> cycles = [];
        int start = 0;
        int offset = 0;

        foreach (int frames in cycleLengths)
        {
            int lead = start == 0 ? vae.TemporalFactor + 1 : vae.TemporalFactor;
            int repeats = ChooseRepeats(canvas, offset, frames);
            int body = frames * GetLoops(frames) * repeats;
            int tail = vae.TemporalFactor - (body % vae.TemporalFactor);
            RefineCycle cycle = new(start, lead, frames, repeats, lead + body + tail);

            cycles.Add(cycle);

            start += cycle.Length;
            offset += frames;
        }

        return cycles;
    }

    private int ChooseRepeats(torch.Tensor canvas, int offset, int frames)
    {
        if (MeasureMotion(canvas, offset, frames) > options.MotionThreshold)
        {
            return options.FrameRepeatsMotion;
        }
        else
        {
            return options.FrameRepeatsDefault;
        }
    }

    private static double MeasureMotion(torch.Tensor canvas, int offset, int frames)
    {
        using IDisposable scope = torch.NewDisposeScope();

        double change = 0.0;
        long pixels = 0;

        for (int index = 0; index < frames; index++)
        {
            using IDisposable pairScope = torch.NewDisposeScope();

            using torch.Tensor current = canvas.select(0, offset + index);
            using torch.Tensor next = canvas.select(0, offset + ((index + 1) % frames));
            using torch.Tensor currentAlpha = current.narrow(0, 3, 1);
            using torch.Tensor nextAlpha = next.narrow(0, 3, 1);
            using torch.Tensor currentColors = current.narrow(0, 0, 3).mul(currentAlpha);
            using torch.Tensor nextColors = next.narrow(0, 0, 3).mul(nextAlpha);
            using torch.Tensor difference = currentColors.sub(nextColors).abs().mean([0]);
            using torch.Tensor coverageMask = currentAlpha.add(nextAlpha).squeeze(0).gt(0.0);
            using torch.Tensor visibleDifference = difference.masked_select(coverageMask);
            using torch.Tensor total = visibleDifference.sum();

            change += total.item<float>();
            pixels += visibleDifference.numel();
        }

        return pixels > 0 ? change / pixels : 0.0;
    }

    private int GetLoops(int frameCount)
    {
        if (frameCount > 1)
        {
            return (options.MinClipFrames + frameCount - 1) / frameCount;
        }
        else
        {
            return 1;
        }
    }

    private torch.Tensor Restore(RefineClip clip, torch.Tensor canvas, torch.Tensor additiveMask, ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long canvasScale = GetCanvasScale(clip, canvas);
        using torch.Tensor refineCanvas = ScaleCanvas(canvas, additiveMask, canvasScale);

        long height = refineCanvas.shape[2];
        long width = refineCanvas.shape[3];
        long paddedHeight = RoundUp(height, vae.SpatialFactor);
        long paddedWidth = RoundUp(width, vae.SpatialFactor);
        long padLeft = (paddedWidth - width) / 2;
        long padTop = (paddedHeight - height) / 2;
        long[] padding = [padLeft, paddedWidth - width - padLeft, padTop, paddedHeight - height - padTop];

        IReadOnlyList<RefineCycle> cycles = LayoutCycles(refineCanvas, clip.CycleLengths);
        IReadOnlyList<RefineRegion> repaintRegions = repaintConfiguration.Regions.GetValueOrDefault(clip.Id, []);
        RefinePrompt prompt = contexts.GetPrompt(clip.Id);
        torch.Tensor context = contexts.GetPromptContext(prompt.Id);
        List<RefineRegion> promptRegions = [];
        List<torch.Tensor> promptContexts = [context.to(options.ModelDtype, options.Device)];

        foreach (RefinePromptRegion region in prompt.Regions)
        {
            torch.Tensor regionContext = contexts.GetPromptContext(region.Id);

            promptRegions.Add(region.Region);
            promptContexts.Add(regionContext.to(options.ModelDtype, options.Device));
        }

        using torch.Tensor repaintPlane = PaintRegions(clip, repaintRegions, canvasScale, height, width);
        using torch.Tensor promptPlane = PaintRegions(clip, promptRegions, canvasScale, height, width);
        using torch.Tensor paddedRepaintPlane = torch.nn.functional.pad(repaintPlane, padding);
        using torch.Tensor paddedPromptPlane = torch.nn.functional.pad(promptPlane, padding);

        RefinePromptPlane prompts = new(paddedPromptPlane, promptContexts);

        torch.Tensor encodedReference;

        using (torch.NewDisposeScope())
        {
            using torch.Tensor colors = Compose(refineCanvas, additiveMask, key);
            using torch.Tensor paddedColors = torch.nn.functional.pad(colors, padding, PaddingModes.Replicate);
            using torch.Tensor stretchedColors = Stretch(paddedColors, cycles);

            stretchedColors.mul_(2.0).sub_(1.0).clamp_(-1.0, 1.0);

            using torch.Tensor video = stretchedColors.unsqueeze(0).transpose(1, 2).to(options.VaeDtype);

            encodedReference = vae.Encode(video).MoveToOuterDisposeScope();
        }

        using torch.Tensor repaintCells = PoolCells(paddedRepaintPlane, encodedReference.shape[2], encodedReference.shape[3], encodedReference.shape[4]);
        using torch.Tensor repaintTokens = repaintCells.nonzero().reshape(-1);

        torch.Tensor reference;

        if (repaintTokens.shape[0] > 0)
        {
            reference = Repaint(encodedReference, repaintTokens, prompts);
        }
        else
        {
            reference = encodedReference.alias();
        }

        using torch.Tensor unnormalizedReference = vae.Unnormalize(reference);
        using torch.Tensor upsampledReference = upsampler.forward(unnormalizedReference);
        using torch.Tensor initialLatent = vae.Normalize(upsampledReference);
        using torch.Tensor modelReference = reference.to(options.ModelDtype);
        using torch.Tensor modelInitialLatent = initialLatent.to(options.ModelDtype);

        using torch.Tensor coverage = BuildCoverage(refineCanvas, additiveMask, cycles);
        using torch.Tensor paddedCoverage = torch.nn.functional.pad(coverage, padding);
        using torch.Tensor targetKeep = SelectTokens(paddedCoverage, initialLatent.shape[3], initialLatent.shape[4]);
        using torch.Tensor referenceKeep = SelectTokens(paddedCoverage, reference.shape[3], reference.shape[4]);

        using torch.Tensor denoised = Denoise(modelInitialLatent, modelReference, targetKeep, referenceKeep, prompts);
        using torch.Tensor picks = BuildPicks(cycles);
        using torch.Tensor decodedVideo = vae.Decode(denoised.to(options.VaeDtype), picks);
        using torch.Tensor levels = decodedVideo.squeeze(0).transpose(0, 1);

        levels.add_(1.0).div_(2.0).clamp_(0.0, 1.0);

        using torch.Tensor frames = levels
            .narrow(2, padTop * RefineOptions.ModelScale, height * RefineOptions.ModelScale)
            .narrow(3, padLeft * RefineOptions.ModelScale, width * RefineOptions.ModelScale);
        using torch.Tensor hostFrames = frames.cpu();
        using torch.Tensor canvasFrames = ScaleFrames(
            hostFrames,
            canvas.shape[2] * RefineOptions.ModelScale,
            canvas.shape[3] * RefineOptions.ModelScale);

        return canvasFrames.contiguous().MoveToOuterDisposeScope();
    }

    private long GetCanvasScale(RefineClip clip, torch.Tensor canvas)
    {
        long minDimension = Math.Min(canvas.shape[2], canvas.shape[3]);
        long minClipDimension = configurations[clip.Id].MinClipDimension;

        return Math.Max(1, (minClipDimension + minDimension - 1) / minDimension);
    }

    private static torch.Tensor ScaleCanvas(torch.Tensor canvas, torch.Tensor additiveMask, long scale)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (scale > 1)
        {
            long height = canvas.shape[2] * scale;
            long width = canvas.shape[3] * scale;

            using torch.Tensor colors = canvas.narrow(1, 0, 3);
            using torch.Tensor alpha = canvas.narrow(1, 3, 1);
            using torch.Tensor premultipliedPlanes = torch.cat([colors.mul(alpha), alpha], dim: 1);
            using torch.Tensor scaledPlanes = PixelOperations.Resize(premultipliedPlanes, height, width).clamp(0.0, 1.0);
            using torch.Tensor scaledColors = PixelOperations.Resize(colors, height, width).clamp(0.0, 1.0);
            using torch.Tensor scaledAlpha = scaledPlanes.narrow(1, 3, 1);
            using torch.Tensor visibleAlpha = scaledAlpha.masked_fill(scaledAlpha.lt(MinScaledAlpha), 0.0);
            using torch.Tensor straightColors = scaledPlanes.narrow(1, 0, 3).div(visibleAlpha.clamp_min(MinScaledAlpha)).clamp(0.0, 1.0);
            using torch.Tensor additiveFrames = additiveMask.gt(0.0);
            using torch.Tensor refineColors = torch.where(additiveFrames, scaledColors, straightColors);

            return torch.cat([refineColors, visibleAlpha], dim: 1).MoveToOuterDisposeScope();
        }
        else
        {
            return canvas.alias().MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor ScaleFrames(torch.Tensor frames, long height, long width)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (frames.shape[2] != height || frames.shape[3] != width)
        {
            return PixelOperations.Resize(frames, height, width).MoveToOuterDisposeScope();
        }
        else
        {
            return frames.alias().MoveToOuterDisposeScope();
        }
    }

    private torch.Tensor Repaint(torch.Tensor reference, torch.Tensor repaintTokens, RefinePromptPlane prompts)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long frames = reference.shape[2];
        long latentHeight = reference.shape[3];
        long latentWidth = reference.shape[4];
        long tokens = frames * latentHeight * latentWidth;
        long repaintedTokens = repaintTokens.shape[0];

        using torch.Tensor modelReference = reference.to(options.ModelDtype);
        using torch.Tensor clean = Patchify(modelReference);
        using torch.Tensor positions = LtxPositions.Build(
            frames, latentHeight, latentWidth, vae.TemporalFactor, vae.SpatialFactor, 1, options.FramesPerSecond, clean.device);
        using torch.Tensor keyframes = torch.arange(tokens, torch.ScalarType.Int64, clean.device)
            .lt(latentHeight * latentWidth)
            .to(torch.ScalarType.Float32)
            .view(1, -1, 1);
        using torch.Tensor timestepIndex = torch.ones([tokens], torch.ScalarType.Int64, clean.device)
            .index_fill(0, repaintTokens, 0)
            .view(1, -1);
        using torch.Tensor tokenPrompts = PoolCells(prompts.Plane, frames, latentHeight, latentWidth);
        using torch.Tensor noise = StableNoise.Generate(options.Seed, [1, tokens, clean.shape[2]], clean.dtype, options.Device);
        using torch.Tensor repaintNoise = noise.index_select(1, repaintTokens);
        using torch.Tensor covered = clean.index_select(1, repaintTokens);

        double firstSigma = LtxSchedule.Distilled.GetSigma(0);

        using torch.Tensor floatCovered = covered.to(torch.ScalarType.Float32);
        using torch.Tensor floatNoise = repaintNoise.to(torch.ScalarType.Float32);
        using torch.Tensor noisyCovered = floatCovered.add(floatNoise.sub(floatCovered).mul(firstSigma));

        torch.Tensor latents = noisyCovered.to(clean.dtype);

        for (int step = 0; step < LtxSchedule.Distilled.Steps; step++)
        {
            using IDisposable stepScope = torch.NewDisposeScope();

            double sigma = LtxSchedule.Distilled.GetSigma(step);

            using torch.Tensor values = torch.tensor([(float)sigma, 0.0f], torch.ScalarType.Float32, clean.device);
            using torch.Tensor sigmaTensor = torch.tensor([(float)sigma], torch.ScalarType.Float32, clean.device);
            using torch.Tensor sequence = clean.index_copy(1, repaintTokens, latents);
            using torch.Tensor velocity = Predict(
                sequence,
                new LtxTimestepSelection(values, timestepIndex),
                sigmaTensor,
                positions,
                keyframes,
                prompts.Contexts,
                tokenPrompts);

            using torch.Tensor repaintVelocity = velocity.index_select(1, repaintTokens);
            using torch.Tensor timesteps = torch.full([1, repaintedTokens], sigma, torch.ScalarType.Float32, clean.device);
            using torch.Tensor denoised = LtxSchedule.Distilled.Denoise(latents, repaintVelocity, timesteps);

            torch.Tensor next = LtxSchedule.Distilled.Step(latents, denoised, step);

            latents.Dispose();
            latents = next.MoveToOuterDisposeScope();
        }

        using torch.Tensor finalLatents = latents;
        using torch.Tensor paintedTokens = clean.index_copy(1, repaintTokens, finalLatents);
        using torch.Tensor painted = Unpatchify(paintedTokens, frames, latentHeight, latentWidth);

        return painted.to(reference.dtype).MoveToOuterDisposeScope();
    }

    private torch.Tensor Denoise(
        torch.Tensor initialLatent,
        torch.Tensor reference,
        torch.Tensor targetKeep,
        torch.Tensor referenceKeep,
        RefinePromptPlane prompts)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long frames = initialLatent.shape[2];
        long latentHeight = initialLatent.shape[3];
        long latentWidth = initialLatent.shape[4];
        long targetTokens = frames * latentHeight * latentWidth;
        long referenceTokens = reference.shape[2] * reference.shape[3] * reference.shape[4];
        long keptTargetTokens = targetKeep.shape[0];
        long keptReferenceTokens = referenceKeep.shape[0];

        using torch.Tensor target = Patchify(initialLatent);
        using torch.Tensor fullReference = Patchify(reference);
        using torch.Tensor referenceSequence = fullReference.index_select(1, referenceKeep);

        using torch.Tensor targetPositions = LtxPositions.Build(
            frames,
            latentHeight,
            latentWidth,
            vae.TemporalFactor,
            vae.SpatialFactor,
            1,
            options.FramesPerSecond,
            initialLatent.device);

        using torch.Tensor referencePositions = LtxPositions.Build(
            reference.shape[2],
            reference.shape[3],
            reference.shape[4],
            vae.TemporalFactor,
            vae.SpatialFactor,
            ReferenceScale,
            options.FramesPerSecond,
            initialLatent.device);

        using torch.Tensor keptTargetPositions = targetPositions.index_select(2, targetKeep);
        using torch.Tensor keptReferencePositions = referencePositions.index_select(2, referenceKeep);
        using torch.Tensor positions = torch.cat([keptTargetPositions, keptReferencePositions], dim: 2);

        using torch.Tensor firstFrame = targetKeep.lt(latentHeight * latentWidth);
        using torch.Tensor targetKeyframes = firstFrame.to(torch.ScalarType.Float32);
        using torch.Tensor referenceKeyframes = torch.zeros([keptReferenceTokens], torch.ScalarType.Float32, initialLatent.device);
        using torch.Tensor keyframes = torch.cat([targetKeyframes, referenceKeyframes], dim: 0).view(1, -1, 1);
        using torch.Tensor timestepIndex = torch.cat(
            [
                torch.zeros([keptTargetTokens], torch.ScalarType.Int64, initialLatent.device),
                torch.ones([keptReferenceTokens], torch.ScalarType.Int64, initialLatent.device)
            ],
            dim: 0).view(1, -1);

        using torch.Tensor targetPrompts = PoolCells(prompts.Plane, frames, latentHeight, latentWidth);
        using torch.Tensor keptTargetPrompts = targetPrompts.index_select(0, targetKeep);
        using torch.Tensor referencePrompts = torch.zeros([keptReferenceTokens], torch.ScalarType.Int64, initialLatent.device);
        using torch.Tensor tokenPrompts = torch.cat([keptTargetPrompts, referencePrompts], dim: 0);

        using torch.Tensor noise = StableNoise.Generate(
            options.Seed,
            [1, targetTokens + referenceTokens, initialLatent.shape[1]],
            initialLatent.dtype,
            options.Device);
        using torch.Tensor fullTargetNoise = noise.narrow(1, 0, targetTokens);
        using torch.Tensor targetNoise = fullTargetNoise.index_select(1, targetKeep);
        using torch.Tensor keptTarget = target.index_select(1, targetKeep);

        double firstSigma = options.Schedule.GetSigma(0);

        using torch.Tensor floatTarget = keptTarget.to(torch.ScalarType.Float32);
        using torch.Tensor floatNoise = targetNoise.to(torch.ScalarType.Float32);
        using torch.Tensor noisyTarget = floatTarget.add(floatNoise.sub(floatTarget).mul(firstSigma));

        torch.Tensor latents = noisyTarget.to(initialLatent.dtype);

        for (int step = 0; step < options.Schedule.Steps; step++)
        {
            using IDisposable stepScope = torch.NewDisposeScope();

            double sigma = options.Schedule.GetSigma(step);

            using torch.Tensor values = torch.tensor([(float)sigma, 0.0f], torch.ScalarType.Float32, initialLatent.device);
            using torch.Tensor sigmaTensor = torch.tensor([(float)sigma], torch.ScalarType.Float32, initialLatent.device);
            using torch.Tensor sequence = torch.cat([latents, referenceSequence], dim: 1);
            using torch.Tensor velocity = Predict(
                sequence,
                new LtxTimestepSelection(values, timestepIndex),
                sigmaTensor,
                positions,
                keyframes,
                prompts.Contexts,
                tokenPrompts);

            using torch.Tensor targetVelocity = velocity.narrow(1, 0, keptTargetTokens);
            using torch.Tensor timesteps = torch.full([1, keptTargetTokens], sigma, torch.ScalarType.Float32, initialLatent.device);
            using torch.Tensor denoised = options.Schedule.Denoise(latents, targetVelocity, timesteps);

            torch.Tensor next = options.Schedule.Step(latents, denoised, step);

            latents.Dispose();
            latents = next.MoveToOuterDisposeScope();
        }

        using torch.Tensor finalLatents = latents;
        using torch.Tensor outputTokens = target.clone();

        outputTokens.index_copy_(1, targetKeep, finalLatents);

        return Unpatchify(outputTokens, frames, latentHeight, latentWidth).MoveToOuterDisposeScope();
    }

    private torch.Tensor Predict(
        torch.Tensor sequence,
        LtxTimestepSelection timesteps,
        torch.Tensor sigma,
        torch.Tensor positions,
        torch.Tensor keyframes,
        IReadOnlyList<torch.Tensor> promptContexts,
        torch.Tensor tokenPrompts)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor velocity = transformer.Forward(sequence, timesteps, sigma, positions, promptContexts[0], keyframes);

        for (int prompt = 1; prompt < promptContexts.Count; prompt++)
        {
            using torch.Tensor promptVelocity = transformer.Forward(sequence, timesteps, sigma, positions, promptContexts[prompt], keyframes);
            using torch.Tensor promptTokens = tokenPrompts.eq(prompt).view(1, -1, 1);

            velocity = torch.where(promptTokens, promptVelocity, velocity);
        }

        return velocity.MoveToOuterDisposeScope();
    }

    private static torch.Tensor BuildPicks(IReadOnlyList<RefineCycle> cycles)
    {
        using IDisposable scope = torch.NewDisposeScope();

        List<torch.Tensor> picks = [];

        foreach (RefineCycle cycle in cycles)
        {
            int first = cycle.Start + cycle.Lead;

            picks.Add(torch.arange(
                first + cycle.Repeats - 1,
                first + (cycle.Frames * cycle.Repeats),
                cycle.Repeats,
                torch.ScalarType.Int64));
        }

        return torch.cat(picks, dim: 0).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Stretch(torch.Tensor frames, IReadOnlyList<RefineCycle> cycles)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int frameCount = GetCyclesFrameCount(cycles);
        long[] sources = new long[frameCount];
        int offset = 0;
        int start = 0;

        foreach (RefineCycle cycle in cycles)
        {
            long period = (long)cycle.Frames * cycle.Repeats;
            long phase = (period - (cycle.Lead % period)) % period;

            for (int position = 0; position < cycle.Length; position++)
            {
                sources[start + position] = offset + (((phase + position) % period) / cycle.Repeats);
            }

            offset += cycle.Frames;
            start += cycle.Length;
        }

        using torch.Tensor index = torch.tensor(sources, torch.ScalarType.Int64, frames.device);

        return frames.index_select(0, index).MoveToOuterDisposeScope();
    }

    private torch.Tensor BuildCoverage(torch.Tensor canvas, torch.Tensor additiveMask, IReadOnlyList<RefineCycle> cycles)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int paddedFrames = GetCyclesFrameCount(cycles);

        using torch.Tensor colors = canvas.narrow(1, 0, 3);
        using torch.Tensor alpha = canvas.narrow(1, 3, 1);
        using torch.Tensor brightness = colors.amax([1], keepdim: true);
        using torch.Tensor additiveFrames = additiveMask.gt(0.0);
        using torch.Tensor visibility = torch.where(additiveFrames, brightness, alpha);
        using torch.Tensor stretchedVisibility = Stretch(visibility, cycles);
        using torch.Tensor firstVisibility = stretchedVisibility.narrow(0, 0, 1);
        using torch.Tensor restVisibility = stretchedVisibility
            .narrow(0, 1, paddedFrames - 1)
            .view(-1, vae.TemporalFactor, 1, canvas.shape[2], canvas.shape[3])
            .amax([1]);
        using torch.Tensor latentVisibility = torch.cat([firstVisibility, restVisibility], dim: 0);

        return latentVisibility.gt(0.0).to(torch.ScalarType.Float32).MoveToOuterDisposeScope();
    }

    private torch.Tensor SelectTokens(torch.Tensor coverage, long gridHeight, long gridWidth)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long cellHeight = coverage.shape[2] / gridHeight;
        long cellWidth = coverage.shape[3] / gridWidth;

        using torch.Tensor cells = torch.nn.functional.max_pool2d(coverage, [cellHeight, cellWidth], [cellHeight, cellWidth]);
        using torch.Tensor dilatedCells = torch.nn.functional.max_pool2d(cells, [3, 3], [1, 1], [1, 1]);
        using torch.Tensor indices = dilatedCells.reshape(-1).gt(0.0).nonzero().reshape(-1);

        return indices.to(options.Device).MoveToOuterDisposeScope();
    }

    private torch.Tensor PoolCells(torch.Tensor plane, long frames, long gridHeight, long gridWidth)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long cellHeight = plane.shape[2] / gridHeight;
        long cellWidth = plane.shape[3] / gridWidth;

        using torch.Tensor cells = torch.nn.functional.max_pool2d(plane, [cellHeight, cellWidth], [cellHeight, cellWidth]);
        using torch.Tensor frameCells = cells.reshape(1, -1).expand(frames, -1).reshape(-1);

        return frameCells.to(torch.ScalarType.Int64).to(options.Device).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Patchify(torch.Tensor latent)
    {
        using IDisposable scope = torch.NewDisposeScope();

        return latent
            .permute(0, 2, 3, 4, 1)
            .reshape(latent.shape[0], -1, latent.shape[1])
            .contiguous()
            .MoveToOuterDisposeScope();
    }

    private static torch.Tensor Unpatchify(torch.Tensor tokens, long frames, long height, long width)
    {
        using IDisposable scope = torch.NewDisposeScope();

        return tokens
            .view(tokens.shape[0], frames, height, width, tokens.shape[2])
            .permute(0, 4, 1, 2, 3)
            .contiguous()
            .MoveToOuterDisposeScope();
    }

    private UmbraSprite CutFrame(
        torch.Tensor frameColors,
        torch.Tensor canvas,
        RefineClipFrame clipFrame,
        int position,
        ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        (UmbraSpriteMetadata metadata, ReadOnlyMemory<byte> pixels) = clipFrame.Frame.Sprite;

        torch.Tensor frameHint = ResolveHint(canvas, clipFrame, position, options.Device);
        torch.Tensor frameSource = ResolveSource(canvas, position);
        torch.Tensor cutColors = Cut(frameColors, clipFrame);
        torch.Tensor cutHint = Cut(frameHint, clipFrame);
        torch.Tensor cutSource = Cut(frameSource, clipFrame);
        torch.Tensor colors;

        if (metadata.Additive)
        {
            colors = TransferDetail(cutColors, cutSource, metadata.PixelScale);
        }
        else
        {
            colors = cutColors;
        }

        torch.Tensor hostHint = cutHint.cpu();
        torch.Tensor silhouette;

        if (IsOpaque(pixels.Span))
        {
            silhouette = hostHint;
        }
        else
        {
            silhouette = EraseChromaKey(colors, hostHint, cutSource, key);
        }

        torch.Tensor planes = torch.cat([colors, silhouette], dim: 0);

        UmbraSpriteMetadata newMetadata = metadata with
        {
            PixelWidth = (int)planes.shape[2],
            PixelHeight = (int)planes.shape[1]
        };

        return new UmbraSprite(newMetadata, PixelTensor.ToPixels(planes));
    }

    private torch.Tensor TransferDetail(torch.Tensor refinedColors, torch.Tensor sourceColors, int pixelScale)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long radius = (long)options.AdditiveDetailRadius * pixelScale * RefineOptions.ModelScale;

        using torch.Tensor sourceLight = LowPass(sourceColors, radius);
        using torch.Tensor refinedLight = LowPass(refinedColors, radius);
        using torch.Tensor refinedDetail = refinedColors.div(refinedLight.clamp_min(MinimumLight));

        return sourceLight.mul(refinedDetail).clamp(0.0, 1.0).MoveToOuterDisposeScope();
    }

    private static torch.Tensor LowPass(torch.Tensor planes, long radius)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long kernel = (2 * radius) + 1;

        using torch.Tensor batch = planes.unsqueeze(0);
        using torch.Tensor boxed = torch.nn.functional.avg_pool2d(batch, [kernel, kernel], [1, 1], [radius, radius]);
        using torch.Tensor smoothed = torch.nn.functional.avg_pool2d(boxed, [kernel, kernel], [1, 1], [radius, radius]);

        return smoothed.squeeze(0).MoveToOuterDisposeScope();
    }

    private static bool IsOpaque(ReadOnlySpan<byte> pixels)
    {
        for (int offset = 3; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset] != byte.MaxValue)
            {
                return false;
            }
        }

        return true;
    }

    private torch.Tensor EraseChromaKey(torch.Tensor colors, torch.Tensor silhouette, torch.Tensor sourceColors, ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor keyPlanes = key.ToPlanes(colors.device);
        using torch.Tensor refinedDistance = colors.sub(keyPlanes).square().sum(0, keepdim: true).sqrt();
        using torch.Tensor sourceDistance = sourceColors.sub(keyPlanes).square().sum(0, keepdim: true).sqrt();
        using torch.Tensor screenMask = refinedDistance.le(options.ChromaKeyScreenThreshold);
        using torch.Tensor objectMask = sourceDistance.ge(options.ChromaKeyObjectThreshold);
        using torch.Tensor solidMask = silhouette.ge(1.0);
        using torch.Tensor erasedMask = screenMask.logical_and(objectMask).logical_and(solidMask);

        return silhouette.masked_fill(erasedMask, 0.0).MoveToOuterDisposeScope();
    }

    private static torch.Tensor ResolveSource(torch.Tensor canvas, int position)
    {
        torch.Tensor colors = canvas.narrow(1, 0, 3).narrow(0, position, 1);
        torch.Tensor scaledColors = torch.nn.functional.interpolate(
            colors, scale_factor: [RefineOptions.ModelScale, RefineOptions.ModelScale], mode: torch.InterpolationMode.Nearest);

        return scaledColors.squeeze(0);
    }

    private static torch.Tensor ResolveHint(torch.Tensor canvas, RefineClipFrame clipFrame, int position, torch.Device device)
    {
        UmbraSpriteMetadata metadata = clipFrame.Frame.Sprite.Metadata;

        if (metadata.Additive)
        {
            return torch.zeros(
                [1, canvas.shape[2] * RefineOptions.ModelScale, canvas.shape[3] * RefineOptions.ModelScale],
                canvas.dtype,
                device);
        }
        else
        {
            torch.Tensor alpha = canvas.narrow(1, 3, 1);
            torch.Tensor frameAlpha = alpha.narrow(0, position, 1).to(device);
            torch.Tensor hint = PixelOperations.Antialias(frameAlpha, RefineOptions.ModelScale);

            return hint.squeeze(0);
        }
    }

    private static torch.Tensor Cut(torch.Tensor frame, RefineClipFrame clipFrame)
    {
        UmbraSpriteMetadata metadata = clipFrame.Frame.Sprite.Metadata;
        torch.Tensor rows = frame.narrow(1, clipFrame.Top * RefineOptions.ModelScale, metadata.PixelHeight * RefineOptions.ModelScale);

        return rows.narrow(2, clipFrame.Left * RefineOptions.ModelScale, metadata.PixelWidth * RefineOptions.ModelScale);
    }
}
