using System;
using Godot;
using Microsoft.Extensions.Logging;
using tps.contract;
using tps.csharp;
using tps.Logging;
using VitalRouter;

namespace tps;

[Routes]
public partial class Target : StaticBody3D
{
    [Export] public int MaxHp = 3;
    [Export] public float RespawnDelay = 3f;

    private Entity _entity = null!;
    private HealthSystem _healthSystem = null!;
    private Router _router = null!;
    private MeshInstance3D _mesh = null!;
    private CollisionShape3D _collision = null!;
    private Timer _respawnTimer = null!;
    private IDisposable? _subscription;
    private readonly ILogger<Target> _logger = AppLogger.For<Target>();

    private static readonly Color AliveColor = new(0.9f, 0.3f, 0.1f);

    public override void _Ready()
    {
        _mesh = GetNode<MeshInstance3D>("Mesh");
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");

        _respawnTimer = new Timer { OneShot = true, WaitTime = RespawnDelay };
        _respawnTimer.Timeout += Respawn;
        AddChild(_respawnTimer);

        SetAliveAppearance();
        AddToGroup("targets");
    }

    public void Initialize(Entity entity, HealthSystem healthSystem, Router router)
    {
        _entity = entity;
        _healthSystem = healthSystem;
        _router = router;
        _subscription = this.MapTo(_router);
        _logger.LogDebug("Target ready name={Name} hp={MaxHp}", Name, MaxHp);
    }

    public override void _ExitTree()
    {
        _subscription?.Dispose();
    }

    [Route]
    public void On(TargetHitCommand cmd)
    {
        if (cmd.TargetName != Name) return;
        _healthSystem.TakeDamage(_entity.Id, cmd.Damage);
        var hp = _entity.Get<HealthComponent>();
        _logger.LogDebug("Target hit hp={Hp}/{Max}", hp?.Hp, hp?.MaxHp);
    }

    [Route]
    public void On(TargetDestroyedCommand cmd)
    {
        if (cmd.TargetName != _entity.Id.AsPrimitive()) return;
        _mesh.Visible = false;
        _collision.Disabled = true;
        _respawnTimer.Start();
        _logger.LogDebug("Target destroyed, respawn in {Delay}s", RespawnDelay);
    }

    private void Respawn()
    {
        _healthSystem.Reset(_entity.Id);
        _mesh.Visible = true;
        SetAliveAppearance();
        _collision.Disabled = false;
        _ = _router.PublishAsync(new TargetRespawnedCommand { TargetName = Name });
        _logger.LogDebug("Target respawned name={Name}", Name);
    }

    private void SetAliveAppearance()
    {
        var mat = new StandardMaterial3D { AlbedoColor = AliveColor };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
    }
}
