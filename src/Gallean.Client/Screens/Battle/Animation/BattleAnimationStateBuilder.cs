using Gallean.Common;
using Gallean.Engine;
using static Gallean.Client.BattleRenderState;
using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed partial class BattleAnimationStateBuilder(
    IAssetMetadataProvider assets,
    IGameDefinitionProvider definitions,
    IRandomProvider random,
    BattleViewerSide viewerSide,
    double speedFactor)
    : IBattleEventVisitor<Empty, Empty>
{
    private const int ChanceMax = 100;

    private const int ActionGapFrames = 12;
    private const int ResolvedGapFrames = 15;
    private const int HitpointsChangeFrames = 12;
    private const int FeedbackWindowFrames = 15;
    private const int FeedbackGapFrames = 4;
    private const int FeedbackFadeFrames = 3;
    private const int CorpseFadeFrames = 4;

    private const double LightPauseMilliseconds = 60.0;
    private const double HeavyPauseMilliseconds = 90.0;

    private const int ShakeFrames = 3;
    private const double ShakeSeverityThreshold = 0.5;
    private const double LightShakeAmplitude = 0.8;
    private const double HeavyShakeAmplitude = 2.4;

    private const int RecoilFrames = 5;
    private const double LightMotionScale = 0.5;
    private const double RecoilDistance = 1.0;

    private const int DamagedSoundChance = 30;

    private readonly AnimationRate rate = AnimationRate.Sprites.Scaled(speedFactor);
    private readonly BattleAnimationResolver resolver = new(assets);
    private readonly List<AnimationClip> clips = [];
    private readonly List<BattleAnimationState.EffectEntry> effects = [];
    private readonly List<AnimationSound> sounds = [];
    private readonly List<AssetAnimationKey> animations = [];
    private readonly SortedDictionary<TimeSpan, TimeSpan> pauses = [];
    private readonly SortedDictionary<TimeSpan, double> shakes = [];
    private readonly Dictionary<BattleUnitId, UnitTimeline> timelines = [];
    private readonly HashSet<BattleUnitId> unsummonedUnitIds = [];
    private readonly List<Attack> actionAttacks = [];
    private Attack? currentAttack;
    private Step? currentStep;
    private TimeSpan sequenceEnd;
    private bool battleEnded;

    public BattleAnimationState? Build(BattleRenderState baseState, IReadOnlyList<BattleEvent> events)
    {
        foreach (Unit unit in baseState.Units)
        {
            timelines[unit.Id] = new UnitTimeline(unit);
        }

        foreach (BattleEvent battleEvent in events)
        {
            if (battleEvent is BattleUnitUnsummonedEvent unsummoned)
            {
                unsummonedUnitIds.Add(unsummoned.UnitId);
            }
        }

        foreach (BattleEvent battleEvent in events)
        {
            battleEvent.Accept(this, Empty.Value);

            if (battleEvent is BattleUnitAttackMissedEvent && currentAttack is { Origin: AttackOrigin.Secondary })
            {
                continue;
            }
            else
            {
                (BattleUnitId Unit, Feedback Feedback)? feedback = battleEvent.Accept(UnitFeedbackVisitor.Instance, Empty.Value);

                if (feedback is not null &&
                    timelines.TryGetValue(feedback.Value.Unit, out UnitTimeline? timeline) &&
                    timeline.IsAlive)
                {
                    AddFeedback(timeline, ResolveFeedbackTime(), feedback.Value.Feedback);
                }
            }
        }

        CloseStep();

        if (sequenceEnd == TimeSpan.Zero)
        {
            return null;
        }

        List<BattleAnimationState.Unit> units = [];
        List<AnimationProperty> properties = [];

        foreach (Unit unit in baseState.Units)
        {
            UnitTimeline timeline = timelines[unit.Id];

            properties.AddRange(timeline.Hitpoints);

            if (timeline.Body.Count > 0 ||
                timeline.Form.Count > 0 ||
                timeline.Feedback.Count > 0 ||
                timeline.Motion.Count > 0 ||
                timeline.Status.Count > 0)
            {
                units.Add(new BattleAnimationState.Unit(
                    unit.Id,
                    BuildBody(timeline),
                    [.. timeline.Form.OrderBy(entry => entry.At)],
                    [.. timeline.Feedback.OrderBy(entry => entry.At)],
                    [.. timeline.Motion.OrderBy(entry => entry.At)],
                    [.. timeline.Status.OrderBy(entry => entry.At)]));
            }
        }

        List<AnimationPause> pauseEntries = BuildPauses();
        long pauseTicks = pauseEntries.Sum(pause => pause.Duration.Ticks);

        AnimationSequence sequence = new(
            sequenceEnd + TimeSpan.FromTicks(pauseTicks),
            [.. clips],
            [.. sounds.OrderBy(sound => sound.Start)],
            [.. properties.OrderBy(property => property.Start)],
            pauseEntries);

        return new BattleAnimationState(
            sequence,
            units,
            [.. effects.OrderBy(effect => BattleRenderDepth.GetEffectDepth(effect.Footprint))],
            animations,
            BuildShakes(),
            rate);
    }

    private static IReadOnlyList<BattleAnimationState.BodyEntry> BuildBody(UnitTimeline timeline)
    {
        List<BattleAnimationState.BodyEntry> entries = [.. timeline.Body.OrderBy(entry => entry.At)];
        List<BattleAnimationState.BodyEntry> result = [];

        for (int index = 0; index < entries.Count; index++)
        {
            BattleAnimationState.BodyEntry entry = entries[index];

            result.Add(entry);

            if (entry.Duration > TimeSpan.Zero &&
                entry.Presence == Presence.Alive &&
                !IsContinuation(index, entry.At + entry.Duration))
            {
                result.Add(new BattleAnimationState.BodyEntry(
                    entry.At + entry.Duration,
                    TimeSpan.Zero,
                    AssetAnimationKey.BattleUnitPose.Idle,
                    entry.Presence,
                    Opacity: null));
            }
        }

        return result;

        bool IsContinuation(int index, TimeSpan at)
        {
            return index + 1 < entries.Count && entries[index + 1].At <= at;
        }
    }

    private List<AnimationPause> BuildPauses()
    {
        List<AnimationPause> result = [];

        foreach ((TimeSpan at, TimeSpan duration) in pauses)
        {
            result.Add(new AnimationPause(at, duration));
        }

        return result;
    }

    private List<BattleAnimationState.ShakeEntry> BuildShakes()
    {
        TimeSpan duration = rate.GetDuration(ShakeFrames);
        List<BattleAnimationState.ShakeEntry> result = [];

        foreach ((TimeSpan at, double amplitude) in shakes)
        {
            result.Add(new BattleAnimationState.ShakeEntry(at, duration, amplitude));
        }

        return result;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleAutoResolveStepEvent ev, Empty context)
    {
        CloseStep();

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleEndedEvent ev, Empty context)
    {
        battleEnded = true;

        OpenStep(rate.GetDuration(ResolvedGapFrames));

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleItemUsedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            ItemDefinition item = definitions.GetItem(ev.ItemId);

            BattleSide targetSide = ev.Target.Accept(AttackTargetSideVisitor.Instance, timelines);

            BeginItem(timeline, item, targetSide);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleRoundStartedEvent ev, Empty context)
    {
        CloseStep();

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleRoundEndedEvent ev, Empty context)
    {
        CloseStep();

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.AttackerId, out UnitTimeline? attacker))
        {
            BattleSide targetSide = ev.Target.Accept(AttackTargetSideVisitor.Instance, timelines);

            BeginAttack(attacker, definitions.GetAttack(ev.AttackIds[0]), targetSide);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackHitEvent ev, Empty context)
    {
        if (ev.Damage > 0 &&
            timelines.TryGetValue(ev.AttackerId, out UnitTimeline? attacker) &&
            timelines.TryGetValue(ev.TargetId, out UnitTimeline? target))
        {
            Attack attack = BeginAttack(attacker, definitions.GetAttack(ev.AttackId), target.Unit.Footprint.Side);
            double severity = ResolveHitSeverity(ev.Damage, target.CurrentHitpoints);

            AddStrike(target);
            AddImpact(target, attack, attack.Strike, severity);

            if (attack.SecondStrike is { } secondStrike)
            {
                AddSecondImpact(target, attack, secondStrike, severity);
            }

            AddHitpoints(target, attack.Strike, Math.Max(0, target.CurrentHitpoints - ev.Damage));
            int soundVariant = random.Next(AssetSoundKey.BattleUnitDamaged.VariantCount);
            int soundRoll = random.Next(ChanceMax);

            if (soundRoll < DamagedSoundChance)
            {
                AddSound(attack.Strike, new AssetSoundKey.BattleUnitDamaged(target.Unit.DefinitionId, soundVariant));
            }
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackStepEvent ev, Empty context)
    {
        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackMissedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.AttackerId, out UnitTimeline? attacker) &&
            timelines.TryGetValue(ev.TargetId, out UnitTimeline? target))
        {
            BeginAttack(attacker, definitions.GetAttack(ev.AttackId), target.Unit.Footprint.Side);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackBlockedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.AttackerId, out UnitTimeline? attacker) &&
            timelines.TryGetValue(ev.TargetId, out UnitTimeline? target))
        {
            BeginAttack(attacker, definitions.GetAttack(ev.AttackId), target.Unit.Footprint.Side);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackGrantedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            AddStrike(timeline);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitAttackedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.AttackerId, out UnitTimeline? attacker) &&
            timelines.TryGetValue(ev.TargetId, out UnitTimeline? target))
        {
            BeginAttack(attacker, definitions.GetAttack(ev.AttackId), target.Unit.Footprint.Side);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitCuredEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            AddStrike(timeline);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitDamagedByEffectEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            Step step = OpenStep(TimeSpan.Zero);
            TimeSpan start = step.Start;

            AssetSoundKey? soundKey = resolver.ResolveStatusSound(ev.Effect);
            if (soundKey is null)
            {
                int variant = random.Next(AssetSoundKey.BattleUnitDamaged.VariantCount);
                soundKey = new AssetSoundKey.BattleUnitDamaged(timeline.Unit.DefinitionId, variant);
            }

            AddRecoil(timeline, start);
            AddHitpoints(timeline, start, ev.HitpointsAfter);
            AddSound(start, soundKey);

            if (resolver.ResolveStatus(ev.Effect, timeline.Unit.Footprint.IsLarge) is { } effect)
            {
                AddEffectClip(timeline.Unit.Footprint, start, effect);
            }
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitDefendedEvent ev, Empty context)
    {
        if (timelines.ContainsKey(ev.UnitId))
        {
            OpenStep(rate.GetDuration(ActionGapFrames));
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitDiedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            Step step = EnsureStep(TimeSpan.Zero);
            TimeSpan at = currentAttack?.Strike ?? step.Start;

            TimeSpan deathStart;
            if (timeline.BodyEnd > at)
            {
                deathStart = timeline.BodyEnd;
            }
            else
            {
                (TimeSpan start, TimeSpan duration) = AddRecoil(timeline, at);

                deathStart = start + duration;
            }

            AssetAnimationKey.BattleEffect deathKey = resolver.ResolveDeath(timeline.Unit.DeathAnimation, timeline.Unit.Footprint.IsLarge);
            AssetAnimationMetadata deathMetadata = assets.GetRequiredAnimationMetadata(deathKey);
            TimeSpan deathDuration = rate.GetDuration(deathMetadata.FrameCount);

            AddEffect(new BattleAnimationState.EffectEntry(
                deathKey,
                timeline.Unit.Footprint,
                deathStart,
                deathDuration));

            TimeSpan corpseStart = deathStart + ResolveRevealOffset(deathKey);
            TimeSpan corpseDuration = deathStart + deathDuration - corpseStart;

            AddSound(deathStart, new AssetSoundKey.BattleEffect.Death());

            if (unsummonedUnitIds.Contains(ev.UnitId))
            {
                AddBody(timeline, corpseStart, corpseDuration, AssetAnimationKey.BattleUnitPose.Idle, Presence.Absent);
            }
            else
            {
                TimeSpan corpseFade = rate.GetDuration(CorpseFadeFrames);

                if (corpseFade > corpseDuration)
                {
                    corpseFade = corpseDuration;
                }

                AddCorpse(timeline, corpseStart, corpseDuration, corpseFade);
            }
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitEffectAppliedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.TargetId, out UnitTimeline? timeline))
        {
            if (currentAttack is not null)
            {
                TimeSpan strike = ResolveStrikeTime();

                AddStrike(timeline);

                if (ev.Effect is BattleBlisterStatusEffect)
                {
                    AddEffectClip(timeline.Unit.Footprint, strike, new AssetAnimationKey.BattleEffect.Blister());
                }

                if (resolver.ResolveStatusSound(ev.Effect) is { } sound)
                {
                    AddSound(strike, sound);
                }
            }

            if (ev.Effect is BattleAbstractDisableEffect)
            {
                timeline.Status.Add(new BattleAnimationState.StatusEntry(ResolveFreezeTime(timeline), Frozen: true));
            }
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitEffectDurationDecreasedEvent ev, Empty context)
    {
        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitEffectExpiredEvent ev, Empty context)
    {
        if (ev.Effect is BattleAbstractDisableEffect && timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            TimeSpan at = ResolveFeedbackTime() + rate.GetDuration(FeedbackWindowFrames);

            timeline.Status.Add(new BattleAnimationState.StatusEntry(at, Frozen: false));
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitExperienceGainedEvent ev, Empty context)
    {
        if (!battleEnded)
        {
            return Empty.Value;
        }

        if (ev.ExperienceGained > 0 &&
            timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline) &&
            timeline.IsAlive)
        {
            AddOutcomeFeedback(timeline, ResolveFeedbackTime(), Feedback.Experience(ev.ExperienceGained));
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitHealedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            TimeSpan strike = ResolveStrikeTime();

            if (currentAttack is { } attack)
            {
                if (attack.Definition?.Type is UnitAttackType.DrainLife or UnitAttackType.DrainLifeOverflow)
                {
                    AddEffectClip(timeline.Unit.Footprint, attack.Strike, new AssetAnimationKey.BattleEffect.Heal());
                }
                else
                {
                    AddStrike(timeline);
                    AddSound(attack.Strike, new AssetSoundKey.BattleEffect.Heal());
                }
            }
            else
            {
                AddSound(strike, new AssetSoundKey.BattleEffect.Heal());
            }

            AddHitpoints(timeline, strike, ev.HitpointsAfter);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitModifierAppliedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            AddStrike(timeline);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitRetreatedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            Step step = OpenStep(rate.GetDuration(ActionGapFrames));
            TimeSpan start = step.Start;

            AddTurnAround(timeline, start);
            AddBody(timeline, start, TimeSpan.Zero, AssetAnimationKey.BattleUnitPose.Idle, Presence.Absent);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitRetreatingEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            TimeSpan strike = ResolveStrikeTime();

            AddStrike(timeline);
            AddTurnAround(timeline, strike);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitRevivedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            TimeSpan strike = ResolveStrikeTime();

            AssetAnimationKey.BattleEffect.Status effectKey = new(
                AssetAnimationKey.BattleEffect.StatusType.Revive,
                timeline.Unit.Footprint.IsLarge);

            AddStrike(timeline);
            AddEffectClip(timeline.Unit.Footprint, strike, effectKey);
            AddBody(timeline, strike + ResolveRevealOffset(effectKey), TimeSpan.Zero, AssetAnimationKey.BattleUnitPose.Idle, Presence.Alive);
            AddHitpoints(timeline, strike, ev.HitpointsAfter);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitSummonedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            TimeSpan strike = ResolveStrikeTime();

            AddStrike(timeline);
            AddBody(timeline, strike, TimeSpan.Zero, AssetAnimationKey.BattleUnitPose.Idle, Presence.Alive);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitUnsummonedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline) && timeline.IsAlive)
        {
            Step step = EnsureStep(TimeSpan.Zero);
            TimeSpan at = currentAttack?.Strike ?? step.Start;
            TimeSpan start = timeline.BodyEnd > at ? timeline.BodyEnd : at;

            AssetAnimationKey.BattleEffect.Status effectKey =
                new(AssetAnimationKey.BattleEffect.StatusType.Unsummon, timeline.Unit.Footprint.IsLarge);
            AddEffectClip(timeline.Unit.Footprint, start, effectKey);
            AddBody(timeline, start + ResolveRevealOffset(effectKey), TimeSpan.Zero, AssetAnimationKey.BattleUnitPose.Idle, Presence.Absent);
            AddSound(start, new AssetSoundKey.BattleEffect.Status(AssetSoundKey.BattleEffect.StatusType.Unsummon));
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitTransformAppliedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.TargetId, out UnitTimeline? timeline) && currentAttack is { } attack)
        {
            AddStrike(timeline);
            AddTransform(timeline, attack.Strike, resolver.ResolveTransform(ev.Type, timeline.Unit.Footprint.IsLarge), ev.DefinitionId);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitTransformRestoredEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline) && timeline.IsAlive)
        {
            TimeSpan strike = ResolveStrikeTime();

            AddStrike(timeline);
            AddTransform(timeline, strike, resolver.ResolveTransformRestore(ev.Type, timeline.Unit.Footprint.IsLarge), ev.DefinitionId);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitTurnStartedEvent ev, Empty context)
    {
        CloseStep();

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitTurnSkippedEvent ev, Empty context)
    {
        if (timelines.ContainsKey(ev.UnitId))
        {
            OpenStep(rate.GetDuration(ActionGapFrames));
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitTurnEndingEvent ev, Empty context)
    {
        CloseStep();

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitTurnEndedEvent ev, Empty context)
    {
        CloseStep();

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitUpgradedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            AssetAnimationKey.BattleEffect effectKey = new AssetAnimationKey.BattleEffect.LevelUp(timeline.Unit.Race, timeline.Unit.Footprint.IsLarge);
            AssetAnimationMetadata effectMetadata = assets.GetRequiredAnimationMetadata(effectKey);
            TimeSpan duration = rate.GetDuration(effectMetadata.FrameCount);
            Step step = OpenStep(duration);
            TimeSpan start = step.Start;
            TimeSpan swap = start + ResolveRevealOffset(effectKey);

            AddEffect(new BattleAnimationState.EffectEntry(
                effectKey,
                timeline.Unit.Footprint,
                start,
                duration));

            AddHitpoints(timeline, swap, ev.Hitpoints);
            AddForm(timeline, swap, ev.ToDefinitionId, timeline.CurrentFacing);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitWaitedEvent ev, Empty context)
    {
        if (timelines.ContainsKey(ev.UnitId))
        {
            OpenStep(rate.GetDuration(ActionGapFrames));
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitWardGrantedEvent ev, Empty context)
    {
        if (timelines.TryGetValue(ev.UnitId, out UnitTimeline? timeline))
        {
            AddStrike(timeline);
        }

        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitWardConsumedEvent ev, Empty context)
    {
        return Empty.Value;
    }

    Empty IBattleEventVisitor<Empty, Empty>.Visit(BattleUnitWardExpiredEvent ev, Empty context)
    {
        return Empty.Value;
    }

    private Attack BeginAttack(UnitTimeline attacker, UnitAttackDefinition definition, BattleSide targetSide)
    {
        Attack? existing = FindAttack(attacker, definition.Id);

        if (existing is not null)
        {
            currentAttack = existing;

            return existing;
        }
        else if (currentAttack is { Origin: AttackOrigin.Item } item && item.Actor.Id == attacker.Unit.Id)
        {
            return item;
        }
        else if (currentAttack is { IsUnitAttack: true } attack && attack.Actor.Id == attacker.Unit.Id)
        {
            return BeginSecondaryAttack(attacker, definition);
        }
        else
        {
            return BeginPrimaryAttack(attacker, definition, targetSide);
        }
    }

    private Attack BeginPrimaryAttack(UnitTimeline attacker, UnitAttackDefinition definition, BattleSide targetSide)
    {
        UnitDefinitionId attackerId = attacker.Unit.DefinitionId;
        AssetAnimationKey poseKey = new AssetAnimationKey.BattleUnit(attackerId, AssetAnimationKey.BattleUnitPose.Attack, attacker.Unit.Footprint.Side);
        AssetAnimationMetadata poseMetadata = assets.GetRequiredAnimationMetadata(poseKey);
        TimeSpan soundOffset = GetSoundOffset(new AssetSoundKey.BattleUnitAttack(attackerId, Variant: 0));
        Step step = OpenStep(TimeSpan.Zero);
        TimeSpan start = step.Start;

        IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> hitEffect =
            ResolveAttackEffect(attackerId, AssetAnimationKey.BattleEffect.AttackType.Hit, targetSide);

        IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> touchEffect =
            resolver.ResolveAttackLayers(attackerId, AssetAnimationKey.BattleEffect.AttackType.Touch, targetSide);

        AssetAnimationKey? hitKey = FindBodyLayer(hitEffect);
        AssetAnimationKey? touchKey = FindBodyLayer(touchEffect);

        TimeSpan hitTime = start + GetTriggerOffset(poseKey);
        TimeSpan touchTime = hitTime + GetTriggerOffset(hitKey);
        TimeSpan strikeTime = touchTime + GetTriggerOffset(touchKey);
        TimeSpan? secondStrikeTime = GetSecondStrike(touchKey ?? hitKey ?? poseKey, strikeTime);

        currentAttack = new Attack(attacker.Unit, AttackOrigin.Primary, attackerId, touchTime, strikeTime, secondStrikeTime, definition);
        actionAttacks.Add(currentAttack);

        AddHitEffect(attackerId, hitEffect, targetSide, hitTime);

        AddBody(attacker, start, rate.GetDuration(poseMetadata.FrameCount), AssetAnimationKey.BattleUnitPose.Attack, Presence.Alive);
        int soundVariant = random.Next(AssetSoundKey.BattleUnitAttack.VariantCount);
        AddSound(start + soundOffset, new AssetSoundKey.BattleUnitAttack(attackerId, soundVariant));

        return currentAttack;
    }

    private void AddHitEffect(
        UnitDefinitionId attackerId,
        IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> effectKeys,
        BattleSide targetSide,
        TimeSpan at)
    {
        if (effectKeys.Count > 0)
        {
            foreach (AssetAnimationKey.BattleEffect.Attack effectKey in effectKeys)
            {
                AssetAnimationMetadata effectMetadata = assets.GetRequiredAnimationMetadata(effectKey);

                AddClip(new AnimationClip(
                    effectKey,
                    BattleRenderLayout.GetGridPlacement(targetSide),
                    at,
                    rate.GetDuration(effectMetadata.FrameCount)));
            }

            int soundVariant = random.Next(AssetSoundKey.BattleUnitHit.VariantCount);
            AddSound(at, new AssetSoundKey.BattleUnitHit(attackerId, soundVariant));
        }
    }

    private static AssetAnimationKey.BattleEffect.Attack? FindBodyLayer(IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> layers)
    {
        foreach (AssetAnimationKey.BattleEffect.Attack layer in layers)
        {
            if (layer.Layer == AssetAnimationKey.BattleLayer.Body)
            {
                return layer;
            }
        }

        if (layers.Count > 0)
        {
            throw new InvalidOperationException($"Attack effect '{layers[0].CanonicalId}' has unexpected data.");
        }

        return null;
    }

    private TimeSpan GetTriggerOffset(AssetAnimationKey? clip)
    {
        if (clip is null)
        {
            return TimeSpan.Zero;
        }
        else
        {
            (int frame, _) = GetTriggerFrames(clip);

            return rate.GetDuration(frame);
        }
    }

    private TimeSpan? GetSecondStrike(AssetAnimationKey strikeClip, TimeSpan strike)
    {
        (int frame, int secondFrame) = GetTriggerFrames(strikeClip);

        if (secondFrame > frame)
        {
            return strike + rate.GetDuration(secondFrame - frame);
        }
        else
        {
            return null;
        }
    }

    private (int Frame, int SecondFrame) GetTriggerFrames(AssetAnimationKey clip)
    {
        AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(clip);

        if (metadata.TriggerFrame is null || metadata.SecondTriggerFrame is null)
        {
            throw new InvalidOperationException($"Animation '{clip.CanonicalId}' has unexpected metadata.");
        }

        int frame = Math.Min(metadata.TriggerFrame.Value, metadata.FrameCount - 1);

        return (frame, metadata.SecondTriggerFrame.Value);
    }

    private Attack BeginSecondaryAttack(UnitTimeline attacker, UnitAttackDefinition definition)
    {
        Step step = EnsureStep(TimeSpan.Zero);
        TimeSpan start = step.End;

        currentAttack = new Attack(attacker.Unit, AttackOrigin.Secondary, effectUnitId: null, start, start, secondStrike: null, definition);
        actionAttacks.Add(currentAttack);

        return currentAttack;
    }

    private Attack? FindAttack(UnitTimeline attacker, UnitAttackDefinitionId attackId)
    {
        foreach (Attack attack in actionAttacks)
        {
            if (attack.Actor.Id == attacker.Unit.Id && attack.Definition?.Id == attackId)
            {
                return attack;
            }
        }

        return null;
    }

    private void BeginItem(UnitTimeline user, ItemDefinition item, BattleSide targetSide)
    {
        Step step = OpenStep(rate.GetDuration(ActionGapFrames));
        TimeSpan start = step.Start;

        if (resolver.ResolveItem(item.Category) is { } effect)
        {
            AddEffectClip(user.Unit.Footprint, start, effect);
        }

        if (ItemSoundResolver.ResolveBattle(item.Category) is { } sound)
        {
            AddSound(start, sound);
        }

        if (item.AnimationUnitId is { } effectUnitId)
        {
            IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> hitEffect =
                ResolveAttackEffect(effectUnitId, AssetAnimationKey.BattleEffect.AttackType.Hit, targetSide);

            IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> touchEffect =
                resolver.ResolveAttackLayers(effectUnitId, AssetAnimationKey.BattleEffect.AttackType.Touch, targetSide);

            AssetAnimationKey? hitKey = FindBodyLayer(hitEffect);
            AssetAnimationKey? touchKey = FindBodyLayer(touchEffect);

            TimeSpan touchTime = start + GetTriggerOffset(hitKey);
            TimeSpan strikeTime = touchTime + GetTriggerOffset(touchKey);

            currentAttack = new Attack(user.Unit, AttackOrigin.Item, effectUnitId, touchTime, strikeTime, secondStrike: null, definition: null);

            AddHitEffect(effectUnitId, hitEffect, targetSide, start);
        }
        else
        {
            currentAttack = new Attack(user.Unit, AttackOrigin.Item, effectUnitId: null, start, start, secondStrike: null, definition: null);
        }
    }

    private TimeSpan ResolveStrikeTime()
    {
        if (currentAttack is not null)
        {
            return currentAttack.Strike;
        }
        else
        {
            Step step = OpenStep(rate.GetDuration(ActionGapFrames));

            return step.Start;
        }
    }

    private TimeSpan ResolveFeedbackTime()
    {
        if (currentAttack is not null)
        {
            return currentAttack.Strike;
        }
        else
        {
            Step step = EnsureStep(TimeSpan.Zero);

            return step.Start;
        }
    }

    private TimeSpan ResolveFreezeTime(UnitTimeline timeline)
    {
        TimeSpan feedback = ResolveFeedbackTime();
        Step step = RequireStep();

        if (timeline.BodyEnd > step.Start && timeline.BodyEnd < feedback)
        {
            return timeline.BodyEnd;
        }
        else
        {
            return feedback;
        }
    }

    private Step OpenStep(TimeSpan minimumDuration)
    {
        CloseStep();

        Step step = new(sequenceEnd, minimumDuration);

        currentStep = step;

        return step;
    }

    private Step EnsureStep(TimeSpan minimumDuration)
    {
        if (currentStep is null)
        {
            return OpenStep(minimumDuration);
        }
        else
        {
            return currentStep;
        }
    }

    private Step RequireStep()
    {
        if (currentStep is null)
        {
            throw new InvalidOperationException("Unexpected battle animation state.");
        }
        else
        {
            return currentStep;
        }
    }

    private void CloseStep()
    {
        if (currentStep is not null)
        {
            sequenceEnd = currentStep.End;
            currentStep = null;
        }

        currentAttack = null;
        actionAttacks.Clear();
    }

    private void AddStrike(UnitTimeline target)
    {
        BattleGridFootprint footprint = target.Unit.Footprint;

        if (currentAttack is { EffectUnitId: { } effectUnitId } attack && attack.StrikeTargets.Add(target.Unit.Id))
        {
            if (attack.StrikeTargets.Count == 1)
            {
                int soundVariant = random.Next(AssetSoundKey.BattleUnitTouch.VariantCount);
                AddSound(attack.Touch, new AssetSoundKey.BattleUnitTouch(effectUnitId, soundVariant));
            }

            AddUnitImpactEffects(attack, effectUnitId, footprint);
        }
    }

    private void AddUnitImpactEffects(Attack attack, UnitDefinitionId effectUnitId, BattleGridFootprint footprint)
    {
        IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> effectKeys =
            ResolveAttackEffect(effectUnitId, AssetAnimationKey.BattleEffect.AttackType.Touch, footprint.Side);

        foreach (AssetAnimationKey.BattleEffect.Attack effectKey in effectKeys)
        {
            AssetAnimationMetadata effectMetadata = assets.GetRequiredAnimationMetadata(effectKey);

            AddEffect(new BattleAnimationState.EffectEntry(
                effectKey,
                footprint,
                attack.Touch,
                rate.GetDuration(effectMetadata.FrameCount)));
        }
    }

    private static double ResolveHitSeverity(int damage, int hitpointsBefore)
    {
        return Math.Clamp(damage / (double)Math.Max(1, hitpointsBefore), 0, 1);
    }

    private TimeSpan ResolvePauseDuration(double severity)
    {
        double milliseconds = double.Lerp(LightPauseMilliseconds, HeavyPauseMilliseconds, severity);

        return TimeSpan.FromMilliseconds(milliseconds / speedFactor);
    }

    private static double GetShakeAmplitude(double severity)
    {
        double weight = (severity - ShakeSeverityThreshold) / (1 - ShakeSeverityThreshold);

        return double.Lerp(LightShakeAmplitude, HeavyShakeAmplitude, weight);
    }

    private void AddPause(TimeSpan at, TimeSpan duration)
    {
        if (!pauses.TryGetValue(at, out TimeSpan existing) || duration > existing)
        {
            pauses[at] = duration;
        }
    }

    private (TimeSpan Start, TimeSpan Duration) AddRecoil(UnitTimeline timeline, TimeSpan at)
    {
        TimeSpan duration = GetPoseDuration(timeline.Unit, AssetAnimationKey.BattleUnitPose.Hit);
        TimeSpan start = ResolveRecoilStart(timeline, at);

        AddBody(timeline, start, duration, AssetAnimationKey.BattleUnitPose.Hit, Presence.Alive);

        return (start, duration);
    }

    private void AddSecondImpact(UnitTimeline target, Attack attack, TimeSpan at, double severity)
    {
        BattleAnimationState.BodyEntry recoil = target.Body[^1];
        TimeSpan offset = at - attack.Strike;

        if (offset <= recoil.Duration)
        {
            TimeSpan lastFrame = recoil.Duration - rate.GetDuration(1);
            TimeSpan duration = offset < lastFrame ? offset : lastFrame;

            target.Body[^1] = recoil with { Duration = duration };
            AddImpact(target, attack, recoil.At + duration, severity);
        }
        else
        {
            AddImpact(target, attack, at, severity);
        }
    }

    private void AddImpact(UnitTimeline target, Attack attack, TimeSpan at, double severity)
    {
        (TimeSpan start, TimeSpan duration) = AddRecoil(target, at);

        if (attack.Definition is { Reach: UnitAttackReach.Adjacent, Type: UnitAttackType.Damage } &&
            attack.Actor.Footprint.Position.IsFrontLine &&
            target.Unit.Footprint.Position.IsFrontLine)
        {
            if (!target.Unit.Footprint.IsLarge)
            {
                double scale = double.Lerp(LightMotionScale, 1, severity);
                TimeSpan recoilDuration = rate.GetDuration(RecoilFrames);
                TimeSpan motionDuration = recoilDuration < duration ? recoilDuration : duration;

                target.Motion.Add(new BattleAnimationState.MotionEntry(
                    start,
                    motionDuration,
                    BattleRenderLayout.GetImpactDirection(attack.Actor.Footprint, target.Unit.Footprint),
                    RecoilDistance * scale));
            }

            TimeSpan pauseDuration = ResolvePauseDuration(severity);
            AddPause(at, pauseDuration);

            bool showShake = target.Unit.Footprint.Side switch
            {
                BattleSide.Attacker => viewerSide is BattleViewerSide.Attacker,
                BattleSide.Defender => viewerSide is BattleViewerSide.Defender
            };

            if (showShake && severity >= ShakeSeverityThreshold)
            {
                double shakeAmplitude = GetShakeAmplitude(severity);
                AddShake(at, shakeAmplitude);
            }
        }
    }

    private void AddShake(TimeSpan at, double amplitude)
    {
        if (!shakes.TryGetValue(at, out double existing) || amplitude > existing)
        {
            shakes[at] = amplitude;
        }
    }

    private static TimeSpan ResolveRecoilStart(UnitTimeline timeline, TimeSpan at)
    {
        if (timeline.Body.Count > 0 &&
            timeline.Body[^1] is { Pose: AssetAnimationKey.BattleUnitPose.Hit } recoil &&
            recoil.At + recoil.Duration > at)
        {
            return recoil.At + recoil.Duration;
        }
        else
        {
            return at;
        }
    }

    private void AddTransform(
        UnitTimeline timeline,
        TimeSpan at,
        AssetAnimationKey.BattleEffect.Status effect,
        UnitDefinitionId definitionId)
    {
        AddEffectClip(timeline.Unit.Footprint, at, effect);
        AddSound(at, new AssetSoundKey.BattleEffect.Status(AssetSoundKey.BattleEffect.StatusType.Transform));
        AddForm(timeline, at + ResolveRevealOffset(effect), definitionId, timeline.CurrentFacing);
    }

    private void AddTurnAround(UnitTimeline timeline, TimeSpan at)
    {
        BattleSide facing = timeline.Unit.Footprint.Side.GetOpposite();

        if (facing != timeline.CurrentFacing)
        {
            AddForm(timeline, at, timeline.CurrentDefinitionId, facing);
        }
    }

    private void AddEffectClip(BattleGridFootprint footprint, TimeSpan at, AssetAnimationKey.BattleEffect effectKey)
    {
        AssetAnimationMetadata effectMetadata = assets.GetRequiredAnimationMetadata(effectKey);

        AddEffect(new BattleAnimationState.EffectEntry(
            effectKey,
            footprint,
            at,
            rate.GetDuration(effectMetadata.FrameCount)));
    }

    private TimeSpan ResolveRevealOffset(AssetAnimationKey.BattleEffect effectKey)
    {
        AssetAnimationMetadata effectMetadata = assets.GetRequiredAnimationMetadata(effectKey);

        return rate.GetDuration(effectMetadata.TriggerFrame ?? (effectMetadata.FrameCount / 2));
    }

    private void AddClip(AnimationClip clip)
    {
        clips.Add(clip);
        animations.Add(clip.Animation);
        RequireStep().ExtendTo(clip.Start + clip.Duration);
    }

    private void AddEffect(BattleAnimationState.EffectEntry effect)
    {
        effects.Add(effect);
        animations.Add(effect.Animation);
        RequireStep().ExtendTo(effect.At + effect.Duration);
    }

    private void AddSound(TimeSpan at, AssetSoundKey key)
    {
        sounds.Add(new AnimationSound(at, key));
        RequireStep().ExtendTo(at);
    }

    private void AddBody(UnitTimeline timeline, TimeSpan at, TimeSpan duration, AssetAnimationKey.BattleUnitPose pose, Presence presence)
    {
        timeline.Body.Add(new BattleAnimationState.BodyEntry(at, duration, pose, presence, Opacity: null));
        RequireStep().ExtendTo(at + duration);

        if (presence == Presence.Alive)
        {
            AddPoseAnimations(timeline.CurrentDefinitionId, pose, timeline.CurrentFacing, timeline.CurrentAuras);
        }
    }

    private void AddPoseAnimations(
        UnitDefinitionId definitionId,
        AssetAnimationKey.BattleUnitPose pose,
        BattleSide facing,
        Auras auras)
    {
        animations.Add(new AssetAnimationKey.BattleUnitShadow(definitionId, pose, facing));
        animations.Add(new AssetAnimationKey.BattleUnit(definitionId, pose, facing));

        AssetAnimationKey.BattleUnitAura? aura = auras.Get(pose);

        if (aura is not null)
        {
            animations.Add(aura);
        }
    }

    private void AddCorpse(UnitTimeline timeline, TimeSpan at, TimeSpan duration, TimeSpan fadeDuration)
    {
        AnimationProperty opacity = new(
            new BattleAnimationTarget.UnitBody(timeline.Unit.Id),
            at,
            fadeDuration,
            0,
            1,
            AnimationEasing.Smooth);

        timeline.Body.Add(new BattleAnimationState.BodyEntry(
            at,
            duration,
            AssetAnimationKey.BattleUnitPose.Idle,
            Presence.Dead,
            opacity));

        RequireStep().ExtendTo(at + duration);
    }

    private void AddHitpoints(UnitTimeline timeline, TimeSpan at, int value)
    {
        TimeSpan duration = rate.GetDuration(HitpointsChangeFrames);

        timeline.Hitpoints.Add(new AnimationProperty(
            new BattleAnimationTarget.UnitHitpoints(timeline.Unit.Id),
            at,
            duration,
            timeline.CurrentHitpoints,
            value,
            AnimationEasing.Decelerate));

        RequireStep().ExtendTo(at + duration);
    }

    private void AddForm(UnitTimeline timeline, TimeSpan at, UnitDefinitionId definitionId, BattleSide facing)
    {
        timeline.Form.Add(new BattleAnimationState.FormEntry(at, definitionId, facing, Auras.Resolve(assets, definitionId, facing)));
        AddPoseAnimations(definitionId, AssetAnimationKey.BattleUnitPose.Idle, facing, timeline.CurrentAuras);

        RequireStep().ExtendTo(at);
    }

    private void AddFeedback(UnitTimeline timeline, TimeSpan at, Feedback feedback)
    {
        TimeSpan window = rate.GetDuration(FeedbackWindowFrames);
        TimeSpan fade = rate.GetDuration(FeedbackFadeFrames);
        BattleAnimationTarget.UnitFeedback target = new(timeline.Unit.Id);

        timeline.Feedback.Add(new BattleAnimationState.FeedbackEntry(
            at,
            window,
            feedback,
            [
                new AnimationProperty(target, at, fade, 0, 1, AnimationEasing.Smooth),
                new AnimationProperty(target, at + window - fade, fade, 1, 0, AnimationEasing.Smooth)
            ]));

        RequireStep().ExtendTo(at + window + rate.GetDuration(FeedbackGapFrames));
    }

    private void AddOutcomeFeedback(UnitTimeline timeline, TimeSpan at, Feedback feedback)
    {
        TimeSpan window = rate.GetDuration(FeedbackWindowFrames);
        TimeSpan fade = rate.GetDuration(FeedbackFadeFrames);
        BattleAnimationTarget.UnitFeedback target = new(timeline.Unit.Id);

        timeline.Feedback.Add(new BattleAnimationState.FeedbackEntry(
            at,
            window,
            feedback,
            [
                new AnimationProperty(target, at, fade, 0, 1, AnimationEasing.Smooth)
            ]));

        RequireStep().ExtendTo(at + window + rate.GetDuration(FeedbackGapFrames));
    }

    private IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> ResolveAttackEffect(
        UnitDefinitionId attackerId,
        AssetAnimationKey.BattleEffect.AttackType type,
        BattleSide targetSide)
    {
        IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> layers = resolver.ResolveAttackLayers(attackerId, type, targetSide);

        List<AssetAnimationKey.BattleEffect.Attack> result = [];

        if (layers.Count > 0)
        {
            IReadOnlyList<int> variants = resolver.ResolveAttackVariants(layers);
            int variant = variants[random.Next(variants.Count)];

            foreach (AssetAnimationKey.BattleEffect.Attack layerKey in layers)
            {
                result.Add(layerKey.WithVariant(variant));
            }
        }

        return result;
    }

    private TimeSpan GetPoseDuration(Unit unit, AssetAnimationKey.BattleUnitPose pose)
    {
        AssetAnimationKey key = new AssetAnimationKey.BattleUnit(unit.DefinitionId, pose, unit.Footprint.Side);
        AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(key);

        return rate.GetDuration(metadata.FrameCount);
    }

    private TimeSpan GetSoundOffset(AssetSoundKey key)
    {
        AssetSoundMetadata? metadata = assets.GetOptionalSoundMetadata(key);

        if (metadata is null)
        {
            return TimeSpan.Zero;
        }
        else if (metadata.Value.AnimationFrame is { } frame)
        {
            return rate.GetDuration(frame);
        }
        else
        {
            throw new InvalidOperationException($"Sound '{key.CanonicalId}' has unexpected metadata.");
        }
    }
}
