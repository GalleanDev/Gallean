using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class DiplomacyProposalViewModel : AbstractGameScreenViewModel
{
    private readonly WorldDiplomacyProposal proposal;

    public DiplomacyProposalViewModel(GameModel gameModel, GameViewModel gameViewModel, WorldDiplomacyProposal proposal)
        : base(gameModel, gameViewModel)
    {
        this.proposal = proposal;

        Message = DiplomacyProposalFormatter.FormatOffer(GameModel.Definitions, GameModel.Texts, WorldState, proposal);
    }

    public string Message { get; }

    protected override bool TryClose()
    {
        DeclineCommand.Execute(null);

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (await TryDispatchWorldActionAsync(new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: true)))
        {
            CloseSelf();
        }
    }

    [RelayCommand]
    private async Task DeclineAsync()
    {
        if (await TryDispatchWorldActionAsync(new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: false)))
        {
            CloseSelf();
        }
    }
}
