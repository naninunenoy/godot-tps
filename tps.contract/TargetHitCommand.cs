using VitalRouter;

namespace tps.contract;

public partial struct TargetHitCommand : ICommand
{
    public string TargetName;
    public int Damage;
}
