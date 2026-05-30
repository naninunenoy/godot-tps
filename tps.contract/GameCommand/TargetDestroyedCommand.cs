using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct TargetDestroyedCommand : ICommand
{
    public string TargetName;
}
