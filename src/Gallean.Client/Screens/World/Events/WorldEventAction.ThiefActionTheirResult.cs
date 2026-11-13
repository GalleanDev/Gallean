using System.Collections.Immutable;
using Gallean.Engine;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record ThiefActionTheirResult(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldThiefActionType Type,
        string ActionName,
        WorldUnitGroup Target,
        bool IsSuccess,
        string TheirLordName) : Screen(Sounds, FocusPosition);
}
