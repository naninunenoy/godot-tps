using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct AdsStateChangedCommand : ICommand
{
    public bool IsAiming;
}
