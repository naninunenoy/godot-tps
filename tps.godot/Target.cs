using System;
using Godot;
using Microsoft.Extensions.Logging;
using R3;
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

    private Health _health = null!;
    private MeshInstance3D _mesh = null!;
    private CollisionShape3D _collision = null!;
    private Timer _respawnTimer = null!;
    private readonly ILogger<Target> _logger = AppLogger.For<Target>();
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _routeSubscription;

    private static readonly Color AliveColor = new(0.9f, 0.3f, 0.1f);

    public override void _Ready()
    {
        GD.Print("Target._Ready() called");
        _mesh = GetNode<MeshInstance3D>("Mesh");
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");

        _health = new Health(MaxHp);
        _health.OnDied.Subscribe(_ => OnDied()).AddTo(_disposables);

        _respawnTimer = new Timer { OneShot = true, WaitTime = RespawnDelay };
        _respawnTimer.Timeout += Respawn;
        AddChild(_respawnTimer);

        SetAliveAppearance();
        _routeSubscription = this.MapTo(GameRouter.Default);
        _logger.LogDebug("Target ready hp={MaxHp}", MaxHp);
    }

    public override void _ExitTree()
    {
        _routeSubscription?.Dispose();
        _disposables.Dispose();
        _health.Dispose();
    }

    [Route]
    public void On(TargetHitCommand cmd)
    {
        if (cmd.TargetName != Name) return;
        TakeDamage(cmd.Damage);
    }

    private void TakeDamage(int damage)
    {
        if (!_health.IsAlive) return;
        _health.TakeDamage(damage);
        _logger.LogDebug("Target hit hp={Current}/{Max}", _health.Current.CurrentValue, _health.Max);
    }

    private void OnDied()
    {
        _mesh.Visible = false;
        _collision.Disabled = true;
        _respawnTimer.Start();
        _ = GameRouter.Default.PublishAsync(new TargetDestroyedCommand { TargetName = Name });
        _logger.LogDebug("Target destroyed, respawn in {Delay}s", RespawnDelay);
    }

    private void Respawn()
    {
        _health.Reset();
        _mesh.Visible = true;
        SetAliveAppearance();
        _collision.Disabled = false;
        _ = GameRouter.Default.PublishAsync(new TargetRespawnedCommand { TargetName = Name });
        _logger.LogDebug("Target respawned name={Name}", Name);
    }

    private void SetAliveAppearance()
    {
        var mat = new StandardMaterial3D { AlbedoColor = AliveColor };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
    }
}
