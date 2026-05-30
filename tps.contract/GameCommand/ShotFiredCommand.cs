using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct ShotFiredCommand : ICommand
{
    public int AmmoLeft;
}
