using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct AimUpdatedCommand : ICommand
{
    public bool IsOnTarget;
}
