using Godot;
using Microsoft.Extensions.Logging;
using tps.contract.GameCommand;
using gamekit.godot.Logging;
using VitalRouter;

namespace tps;

public partial class Bullet : Node3D
{
    [Export]
    public float MaxDistance = 200f;

    private float _speed;
    private int _damage;
    private Router _router = null!;
    private float _distanceTraveled;
    private readonly ILogger<Bullet> _logger = AppLogger.For<Bullet>();

    public void Initialize(Router router, float speed, int damage)
    {
        _router = router;
        _speed = speed;
        _damage = damage;
    }

    public override void _Ready()
    {
        _logger.LogDebug("Bullet spawned pos={Pos}", GlobalPosition);
    }

    public override void _PhysicsProcess(double delta)
    {
        var from = GlobalPosition;
        float step = _speed * (float)delta;
        GlobalPosition += -GlobalTransform.Basis.Z * step;
        _distanceTraveled += step;

        var query = PhysicsRayQueryParameters3D.Create(from, GlobalPosition);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            var colliderId = result["collider_id"].AsUInt64();
            if (GodotObject.InstanceFromId(colliderId) is Target target)
            {
                _ = _router.PublishAsync(new TargetHitCommand { TargetName = target.Name, Damage = _damage });
                _logger.LogDebug("Bullet hit target={Name} damage={Damage}", target.Name, _damage);
                QueueFree();
                return;
            }
        }

        if (_distanceTraveled >= MaxDistance)
        {
            _logger.LogDebug("Bullet expired at dist={Dist}", _distanceTraveled);
            QueueFree();
        }
    }
}
