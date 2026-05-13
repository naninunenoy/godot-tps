using Godot;
using Microsoft.Extensions.Logging;
using tps.Logging;

namespace tps;

public partial class Bullet : Node3D
{
    [Export]
    public float Speed = 80f;

    [Export]
    public float MaxDistance = 200f;

    private readonly ILogger<Bullet> _logger = AppLogger.For<Bullet>();
    private float _distanceTraveled;

    public override void _Ready()
    {
        _logger.LogDebug("Bullet spawned pos={Pos}", GlobalPosition);
    }

    public override void _Process(double delta)
    {
        float step = Speed * (float)delta;
        Position += -Transform.Basis.Z * step;
        _distanceTraveled += step;
        if (_distanceTraveled >= MaxDistance)
        {
            _logger.LogDebug("Bullet expired at dist={Dist}", _distanceTraveled);
            QueueFree();
        }
    }
}
