using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record ThiefActionMineResult(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldThiefActionType Type,
        string ActionName,
        bool IsSuccess) : Screen(Sounds, FocusPosition);
}
