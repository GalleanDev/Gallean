using System.Collections.Immutable;

namespace Gallean.Client;

internal closed partial record WorldEventAction
{
    public sealed record DiplomacyProposal(
        ImmutableArray<AssetSoundKey> Sounds,
        WorldPosition? FocusPosition,
        WorldDiplomacyProposal Proposal) : Screen(Sounds, FocusPosition);
}
