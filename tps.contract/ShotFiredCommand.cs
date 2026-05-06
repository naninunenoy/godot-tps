using VitalRouter;

namespace tps.contract;

public partial struct ShotFiredCommand : ICommand
{
    public int AmmoLeft;
}
