using VitalRouter;

namespace tps.contract;

public partial struct TargetRespawnedCommand : ICommand
{
    public string TargetName;
}
