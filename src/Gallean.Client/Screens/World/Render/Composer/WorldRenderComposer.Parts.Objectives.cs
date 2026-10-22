using System.Collections.Immutable;
using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Objectives
    {
        public readonly record struct Input(
            ImmutableArray<WorldObjective> Objectives,
            WorldVisibility Visibility,
            WorldStackId? AnimatedStackId)
        {
            public bool Equals(Input other)
            {
                return Visibility == other.Visibility &&
                       AnimatedStackId == other.AnimatedStackId &&
                       Objectives.SequenceEqual(other.Objectives);
            }

            public override int GetHashCode()
            {
                HashCode hash = new();

                hash.Add(Visibility);
                hash.Add(AnimatedStackId);

                foreach (WorldObjective objective in Objectives)
                {
                    hash.Add(objective);
                }

                return hash.ToHashCode();
            }
        }

        public static IReadOnlyList<ObjectiveMarker> Build(Input input)
        {
            List<ObjectiveMarker> markers = [];

            foreach (WorldObjective objective in input.Objectives)
            {
                if (objective.StackId is null || objective.StackId != input.AnimatedStackId)
                {
                    if (IsObjectVisible(input.Visibility, objective.Footprint))
                    {
                        ObjectiveMarker marker = new(objective.Footprint, objective.Footprint.Width >= 2);

                        if (!markers.Contains(marker))
                        {
                            markers.Add(marker);
                        }
                    }
                }
            }

            return markers;
        }
    }
}
