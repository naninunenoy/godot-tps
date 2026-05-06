using System.Linq;
using Godot;
using Microsoft.Extensions.Logging;
using tps.csharp;
using tps.Logging;

namespace tps;

public partial class Main : Node3D
{
    private readonly KillCounter _killCounter = new();
    private readonly ILogger<Main> _logger = AppLogger.For<Main>();
    private PauseDialog _pauseDialog = null!;
    private Player _player = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GD.Print("main ready");

        _player = GetNode<Player>("Player");

        var hud = GetNode<Hud>("HudLayer/Hud");
        hud.SetKillCounter(_killCounter);

        _pauseDialog = new PauseDialog();
        GetNode<CanvasLayer>("HudLayer").AddChild(_pauseDialog);
        _pauseDialog.Resumed += Resume;
        _pauseDialog.QuitRequested += () => GetTree().Quit();

        foreach (var target in GetChildren().OfType<Target>())
        {
            target.Destroyed += _killCounter.Increment;
        }
    }

    public override void _ExitTree()
    {
        _killCounter.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Keycode: Key.Escape, Pressed: true }) return;

        if (_pauseDialog.Visible)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        _pauseDialog.Visible = true;
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _logger.LogDebug("Game paused");
    }

    private void Resume()
    {
        _pauseDialog.Visible = false;
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _logger.LogDebug("Game resumed");
    }

    public override void _Process(double delta)
    { }
}
