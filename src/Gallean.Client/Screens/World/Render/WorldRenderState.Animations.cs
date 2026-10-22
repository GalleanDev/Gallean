using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record WorldRenderState
{
    public abstract record WorldAnimation : IAnimationObject
    {
        public abstract TimeSpan Duration { get; }

        public abstract TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context);
    }

    public sealed record StackMoveStep(
        WorldPosition Position,
        int Depth,
        AssetAnimationKey AssetKey,
        AssetAnimationKey? ShadowKey,
        AssetAnimationKey? AuraKey,
        TimeSpan Start);

    public sealed record StackMove(
        WorldStackId StackId,
        AssetSoundKey SoundKey,
        AssetSpriteKey BannerKey,
        IReadOnlyList<StackMoveStep> Steps,
        double Opacity,
        bool IsObjective,
        Color? Highlight) : WorldAnimation
    {
        public override TimeSpan Duration => Steps[^1].Start;

        public override TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public bool Equals(StackMove? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StackId == other.StackId &&
                   SoundKey == other.SoundKey &&
                   BannerKey == other.BannerKey &&
                   Steps.SequenceEqual(other.Steps) &&
                   Opacity == other.Opacity &&
                   IsObjective == other.IsObjective &&
                   Highlight == other.Highlight;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(StackId);
            hash.Add(SoundKey);
            hash.Add(BannerKey);
            hash.AddSequence(Steps);
            hash.Add(Opacity);
            hash.Add(IsObjective);
            hash.Add(Highlight);

            return hash.ToHashCode();
        }
    }

    public sealed record Effect(
        AssetSoundKey? SoundKey,
        WorldPosition Position,
        int Depth,
        AssetAnimationKey AssetKey,
        int FrameCount) : WorldAnimation
    {
        public override TimeSpan Duration => AnimationRate.Sprites.GetDuration(FrameCount);

        public override TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Pause(TimeSpan Duration) : WorldAnimation
    {
        public override TimeSpan Duration { get; } = Duration;

        public override TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record StackBattle(
        WorldPosition Position,
        int Depth,
        AssetAnimationKey AssetKey,
        int FrameCount) : WorldAnimation
    {
        public override TimeSpan Duration => TimeSpan.MaxValue;

        public override TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record BattleTransition(WorldPosition Position, BattleTransitionType Type) : WorldAnimation
    {
        public override TimeSpan Duration => Type switch
        {
            BattleTransitionType.In => TimeSpan.FromSeconds(1.2),
            BattleTransitionType.Out => TimeSpan.FromSeconds(0.8)
        };

        public override TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public abstract record SpellCastLayer(int Depth)
    {
        public sealed record Effect(
            AssetAnimationKey AssetKey,
            int Depth,
            int FrameCount) : SpellCastLayer(Depth)
        {
            public override TResult Accept<TContext, TResult>(ISpellCastLayerVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public sealed record Summon(
            Stack Stack,
            int RevealFrame) : SpellCastLayer(Stack.Depth)
        {
            public override TResult Accept<TContext, TResult>(ISpellCastLayerVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public abstract TResult Accept<TContext, TResult>(ISpellCastLayerVisitor<TContext, TResult> visitor, TContext context);
    }

    public sealed record SpellCast(
        AssetSoundKey SoundKey,
        WorldPosition Position,
        IReadOnlyList<SpellCastLayer> Layers,
        WorldStackId? SummonedStackId,
        int FrameCount) : WorldAnimation
    {
        public override TimeSpan Duration => AnimationRate.Sprites.GetDuration(FrameCount);

        public override TResult Accept<TContext, TResult>(IWorldAnimationVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public bool Equals(SpellCast? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return SoundKey == other.SoundKey &&
                   Position == other.Position &&
                   Layers.SequenceEqual(other.Layers) &&
                   SummonedStackId == other.SummonedStackId &&
                   FrameCount == other.FrameCount;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(SoundKey);
            hash.Add(Position);
            hash.AddSequence(Layers);
            hash.Add(SummonedStackId);
            hash.Add(FrameCount);

            return hash.ToHashCode();
        }
    }
}
