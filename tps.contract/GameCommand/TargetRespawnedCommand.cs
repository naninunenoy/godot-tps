using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct TargetRespawnedCommand : ICommand
{
    public string TargetName;
}
