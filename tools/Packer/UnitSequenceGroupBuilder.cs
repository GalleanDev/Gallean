using static Gallean.Assets.AssetAnimationKey;

namespace Packer;

internal static class UnitSequenceGroupBuilder
{
    public static IEnumerable<UmbraSequenceGroup> Build(
        IEnumerable<string> ids,
        IEnumerable<string> candidateIds,
        Func<string, List<UmbraFrame>> loadFrames)
    {
        List<List<string>> groups = GetGroups(ids, candidateIds);

        foreach (List<string> members in groups)
        {
            List<UmbraSequence> sequences = [];

            foreach (string member in members)
            {
                UmbraSequenceId sequenceId = new(member);

                sequences.Add(new UmbraSequence(sequenceId, loadFrames(member)));
            }

            yield return new UmbraSequenceGroup(new UmbraSequenceId(members[0]), sequences);
        }
    }

    public static List<string> ExpandGroups(IEnumerable<string> ids, IEnumerable<string> candidateIds)
    {
        List<string> result = [];

        foreach (List<string> members in GetGroups(ids, candidateIds))
        {
            result.AddRange(members);
        }

        return result;
    }

    private static List<List<string>> GetGroups(IEnumerable<string> ids, IEnumerable<string> candidateIds)
    {
        SortedDictionary<string, List<string>> groups = new(StringComparer.Ordinal);
        HashSet<string> candidates = [.. candidateIds];

        foreach (string id in ids)
        {
            (string groupId, List<string> members) = GetGroup(id, candidates);

            groups.TryAdd(groupId, members);
        }

        return [.. groups.Values];
    }

    private static (string GroupId, List<string> Members) GetGroup(string id, HashSet<string> candidates)
    {
        if (!TryParse(id, null, out AssetAnimationKey? key))
        {
            return (id, [id]);
        }

        if (key is BattleUnit unit)
        {
            List<string> members = [];

            foreach (BattleUnitPose pose in Enum.GetValues<BattleUnitPose>())
            {
                BattleUnit poseUnit = unit with { Pose = pose };

                if (candidates.Contains(poseUnit.CanonicalId))
                {
                    members.Add(poseUnit.CanonicalId);
                }
            }

            BattleUnit idle = unit with { Pose = BattleUnitPose.Idle };

            return (idle.CanonicalId, members);
        }

        return (id, [id]);
    }
}
