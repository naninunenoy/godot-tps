using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.Extensions.Logging;
using tps.contract;
using tps.csharp;
using tps.Logging;
using VitalRouter;

namespace tps;

[Routes]
public partial class Main : Node3D, ISceneQuery
{
    private Router _router = null!;
    private World _world = null!;
    private IIdGenerator _idGenerator = null!;
    private KillSystem _killSystem = null!;
    private HealthSystem _healthSystem = null!;
    private WeaponSystem _weaponSystem = null!;
    private MovementSystem _movementSystem = null!;
    private IScene _currentScene = null!;
    private readonly Dictionary<EntityId, Entity> _entities = new();
    private IDisposable? _subscription;
    private PauseDialog _pauseDialog = null!;
    private bool _isPaused;
    private readonly ILogger<Main> _logger = AppLogger.For<Main>();

    // ── ISceneQuery ──────────────────────────────────────────────────
    public ulong FrameCount => Engine.GetProcessFrames();
    public int ObjectCount => _entities.Count;
    public IReadOnlyList<IObjectSnapshot> Snapshot =>
        _entities.Values.Select(e => e.Snapshot()).ToList();

    public IReadOnlyList<ICommandDescriptor> GetAvailableCommands() =>
        _currentScene.AvailableCommands;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // インフラ
        _router = new Router();
        _world = new World();
        _idGenerator = new SequentialIdGenerator();

        // Systems
        _killSystem = new KillSystem(_router);
        _healthSystem = new HealthSystem(_world, _router);
        _weaponSystem = new WeaponSystem(_world, _router);
        _movementSystem = new MovementSystem(_world);

        // Player
        var playerEntity = RegisterEntity("player");
        playerEntity.Set(new TransformComponent(SN.Vector3.Zero, SN.Vector3.Zero));
        playerEntity.Set(new WeaponComponent(30, 30, 0f, 0f, 2f, 0.1f));
        GetNode<Player>("Player").Initialize(playerEntity, _weaponSystem, _movementSystem, _router, new PlayerSettings());

        // Targets（"targets" グループに所属するノードを初期化）
        foreach (var target in GetTree().GetNodesInGroup("targets").OfType<Target>())
        {
            var entity = RegisterEntity(target.Name);
            entity.Set(new TransformComponent(SN.Vector3.Zero, SN.Vector3.Zero));
            entity.Set(new HealthComponent(target.MaxHp, target.MaxHp));
            target.Initialize(entity, _healthSystem, _router);
        }

        // UI
        _pauseDialog = GetNode<PauseDialog>("HudLayer/PauseDialog");
        GetNode<Hud>("HudLayer/Hud").Initialize(_router);
        _pauseDialog.Initialize(_router);

        // シーン
        _currentScene = new InGameScene();
        _subscription = this.MapTo(_router);

        _logger.LogDebug("Main ready");
    }

    public override void _ExitTree()
    {
        _subscription?.Dispose();
        _killSystem.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Keycode: Key.Escape, Pressed: true }) return;
        if (_isPaused)
            _ = _router.PublishAsync(new GameResumeRequestedCommand());
        else
            _ = _router.PublishAsync(new GamePauseRequestedCommand());
    }

    [Route]
    public void On(GamePauseRequestedCommand _)
    {
        _isPaused = true;
        _pauseDialog.Visible = true;
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _logger.LogDebug("Game paused");
    }

    [Route]
    public void On(GameResumeRequestedCommand _)
    {
        _isPaused = false;
        _pauseDialog.Visible = false;
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _logger.LogDebug("Game resumed");
    }

    [Route]
    public void On(QuitRequestedCommand _) => GetTree().Quit();

    public override void _Process(double delta) { }

    private Entity RegisterEntity(string name)
    {
        var id = new EntityId(name);
        _world.Register(id);
        var entity = new Entity(id, name, _world);
        _entities[id] = entity;
        return entity;
    }
}
