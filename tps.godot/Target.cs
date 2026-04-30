using Godot;
using Microsoft.Extensions.Logging;
using tps.csharp;
using tps.Logging;

namespace tps;

public partial class Target : StaticBody3D
{
    [Export] public int MaxHp = 3;
    [Export] public float RespawnDelay = 3f;

    private Health _health;
    private MeshInstance3D _mesh;
    private CollisionShape3D _collision;
    private Timer _respawnTimer;
    private readonly ILogger<Target> _logger = AppLogger.For<Target>();

    private static readonly Color AliveColor = new(0.9f, 0.3f, 0.1f);
    private static readonly Color DeadColor = new(0.3f, 0.3f, 0.3f);

    public override void _Ready()
    {
        _mesh = GetNode<MeshInstance3D>("Mesh");
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");

        _health = new Health(MaxHp);
        _health.OnDied += OnDied;

        _respawnTimer = new Timer { OneShot = true, WaitTime = RespawnDelay };
        _respawnTimer.Timeout += Respawn;
        AddChild(_respawnTimer);

        SetAliveAppearance();
        _logger.LogDebug("Target ready hp={MaxHp}", MaxHp);
    }

    public void TakeDamage(int damage)
    {
        if (!_health.IsAlive) return;
        _health.TakeDamage(damage);
        _logger.LogDebug("Target hit hp={Current}/{Max}", _health.Current, _health.Max);
    }

    private void OnDied()
    {
        SetDeadAppearance();
        _collision.Disabled = true;
        _respawnTimer.Start();
        _logger.LogDebug("Target destroyed, respawn in {Delay}s", RespawnDelay);
    }

    private void Respawn()
    {
        _health.Reset();
        SetAliveAppearance();
        _collision.Disabled = false;
        _logger.LogDebug("Target respawned");
    }

    private void SetAliveAppearance()
    {
        var mat = new StandardMaterial3D { AlbedoColor = AliveColor };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
    }

    private void SetDeadAppearance()
    {
        var mat = new StandardMaterial3D { AlbedoColor = DeadColor };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
    }
}
