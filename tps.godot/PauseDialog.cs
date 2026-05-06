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
        ProcessMode = ProcessModeEnum.Always;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.5f) };
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(280, 0) };
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_right", 28);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        margin.AddChild(vbox);

        var label = new Label
        {
            Text = "ゲームを終了しますか？",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        vbox.AddChild(label);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(hbox);

        var resumeBtn = new Button { Text = "戻る", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        resumeBtn.Pressed += () =>
        {
            _logger.LogDebug("PauseDialog: resume");
            _ = GameRouter.Default.PublishAsync(new GameResumeRequestedCommand());
        };
        hbox.AddChild(resumeBtn);

        var quitBtn = new Button { Text = "終了", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        quitBtn.Pressed += () =>
        {
            _logger.LogDebug("PauseDialog: quit");
            _ = GameRouter.Default.PublishAsync(new QuitRequestedCommand());
        };
        hbox.AddChild(quitBtn);

        Visible = false;
        _logger.LogDebug("PauseDialog ready");
    }
}
