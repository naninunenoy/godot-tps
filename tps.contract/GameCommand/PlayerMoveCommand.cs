using System.Numerics;
using VitalRouter;

namespace tps.contract.GameCommand;

public partial struct PlayerMoveCommand : ICommand
{
    public Vector3 Velocity;
}
