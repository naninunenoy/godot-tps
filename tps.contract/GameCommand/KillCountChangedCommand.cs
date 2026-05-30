using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct KillCountChangedCommand : ICommand
{
    public int Count;
}
