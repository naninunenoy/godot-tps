using Godot;
using Microsoft.Extensions.Logging;
using tps.contract.GameCommand;
using tps.Logging;
using VitalRouter;

namespace tps;

public partial class PauseDialog : Control
{
    private readonly ILogger<PauseDialog> _logger = AppLogger.For<PauseDialog>();

    private Button _resumeBtn = null!;
    private Button _quitBtn = null!;

    public override void _Ready()
    {
        _resumeBtn = GetNode<Button>("Center/Panel/Margin/VBox/HBox/ResumeButton");
        _quitBtn = GetNode<Button>("Center/Panel/Margin/VBox/HBox/QuitButton");
        _logger.LogDebug("PauseDialog ready");
    }

    public void Initialize(Router router)
    {
        _resumeBtn.Pressed += () =>
        {
            _logger.LogDebug("PauseDialog: resume");
            _ = router.PublishAsync(new GameResumeRequestedCommand());
        };
        _quitBtn.Pressed += () =>
        {
            _logger.LogDebug("PauseDialog: quit");
            _ = router.PublishAsync(new QuitRequestedCommand());
        };
    }
}
