using System.Numerics;
using VitalRouter;

namespace tps.contract;

public partial struct PlayerMoveCommand : ICommand
{
    public Vector3 Velocity;
}
