using System.Numerics;
using VitalRouter;

namespace tps.contract;

public partial struct BulletSpawnRequested : ICommand
{
    public Vector3 Direction;
    public float Speed;
    public int Damage;
}
