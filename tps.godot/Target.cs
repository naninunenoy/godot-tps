using Godot;
using Microsoft.Extensions.Logging;
using R3;
using tps.csharp;
using tps.Logging;

namespace tps;

public partial class Target : StaticBody3D
{
    [Signal] public delegate void DestroyedEventHandler();
    [Export] public int MaxHp = 3;
    [Export] public float RespawnDelay = 3f;

    private Health _health = null!;
    private MeshInstance3D _mesh = null!;
    private CollisionShape3D _collision = null!;
    private Timer _respawnTimer = null!;
    private readonly ILogger<Target> _logger = AppLogger.For<Target>();
    private readonly CompositeDisposable _disposables = new();

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
        _logger.LogDebug("Target ready hp={MaxHp}", MaxHp);
    }

    public override void _ExitTree()
    {
        _disposables.Dispose();
        _health.Dispose();
    }

    public void TakeDamage(int damage)
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
        EmitSignal(SignalName.Destroyed);
        _logger.LogDebug("Target destroyed, respawn in {Delay}s", RespawnDelay);
    }

    private void Respawn()
    {
        _health.Reset();
        _mesh.Visible = true;
        SetAliveAppearance();
        _collision.Disabled = false;
        _logger.LogDebug("Target respawned");
    }

    private void SetAliveAppearance()
    {
        var mat = new StandardMaterial3D { AlbedoColor = AliveColor };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
    }
}
