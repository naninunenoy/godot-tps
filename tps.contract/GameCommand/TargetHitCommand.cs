using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct TargetHitCommand : ICommand
{
    public string TargetName;
    public int Damage;
}
