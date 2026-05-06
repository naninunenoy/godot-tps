using Godot;
using Microsoft.Extensions.Logging;
using tps.contract;
using tps.csharp;
using tps.Logging;
using VitalRouter;

namespace tps;

public partial class PauseDialog : Control
{
    private readonly ILogger<PauseDialog> _logger = AppLogger.For<PauseDialog>();

    public override void _Ready()
    {
        var resumeBtn = GetNode<Button>("Center/Panel/Margin/VBox/HBox/ResumeButton");
        resumeBtn.Pressed += () =>
        {
            _logger.LogDebug("PauseDialog: resume");
            _ = GameRouter.Default.PublishAsync(new GameResumeRequestedCommand());
        };

        var quitBtn = GetNode<Button>("Center/Panel/Margin/VBox/HBox/QuitButton");
        quitBtn.Pressed += () =>
        {
            _logger.LogDebug("PauseDialog: quit");
            _ = GameRouter.Default.PublishAsync(new QuitRequestedCommand());
        };

        _logger.LogDebug("PauseDialog ready");
    }
}
