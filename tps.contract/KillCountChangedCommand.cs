using VitalRouter;

namespace tps.contract;

public partial struct KillCountChangedCommand : ICommand
{
    public int Count;
}
