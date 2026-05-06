using System;
using Godot;
using Microsoft.Extensions.Logging;
using tps.contract;
using tps.csharp;
using tps.Logging;
using VitalRouter;

namespace tps;

[Routes]
public partial class Main : Node3D
{
    private readonly KillCounter _killCounter = new();
    private readonly ILogger<Main> _logger = AppLogger.For<Main>();
    private IDisposable? _subscription;
    private PauseDialog _pauseDialog = null!;
    private bool _isPaused;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _subscription = this.MapTo(GameRouter.Default);

        _pauseDialog = GetNode<PauseDialog>("HudLayer/PauseDialog");

        _logger.LogDebug("Main ready");
    }

    public override void _ExitTree()
    {
        _subscription?.Dispose();
        _killCounter.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Keycode: Key.Escape, Pressed: true }) return;

        if (_isPaused)
            _ = GameRouter.Default.PublishAsync(new GameResumeRequestedCommand());
        else
            _ = GameRouter.Default.PublishAsync(new GamePauseRequestedCommand());
    }

    [Route]
    public void On(GamePauseRequestedCommand cmd)
    {
        _isPaused = true;
        _pauseDialog.Visible = true;
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _logger.LogDebug("Game paused");
    }

    [Route]
    public void On(GameResumeRequestedCommand cmd)
    {
        _isPaused = false;
        _pauseDialog.Visible = false;
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _logger.LogDebug("Game resumed");
    }

    [Route]
    public void On(QuitRequestedCommand cmd) => GetTree().Quit();

    public override void _Process(double delta) { }
}
