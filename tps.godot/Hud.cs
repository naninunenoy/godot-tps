using System;
using Godot;
using Microsoft.Extensions.Logging;
using tps.contract;
using tps.Logging;
using VitalRouter;

namespace tps;

[Routes]
public partial class Hud : Control
{
    [Export] public float CrossSize = 14f;
    [Export] public float CrossGap  =  4f;
    [Export] public float LineWidth =  2f;
    [Export] public Color CrossColor = new(1f, 1f, 1f, 0.85f);

    private Label _killCountLabel = null!;
    private IDisposable? _subscription;
    private readonly ILogger<Hud> _logger = AppLogger.For<Hud>();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        _killCountLabel = GetNode<Label>("KillCountLabel");
        _killCountLabel.Text = "Kills: 0";
    }

    public void Initialize(Router router)
    {
        _subscription = this.MapTo(router);
        _logger.LogDebug("Hud ready");
    }

    public override void _ExitTree()
    {
        _subscription?.Dispose();
    }

    [Route]
    public void On(KillCountChangedCommand cmd)
    {
        _killCountLabel.Text = $"Kills: {cmd.Count}";
        _logger.LogDebug("Kill count updated: {Count}", cmd.Count);
    }

    public override void _Draw()
    {
        var center = Size / 2f;

        DrawLine(
            center + new Vector2(-(CrossGap + CrossSize), 0),
            center + new Vector2(-CrossGap, 0),
            CrossColor, LineWidth);
        DrawLine(
            center + new Vector2(CrossGap, 0),
            center + new Vector2(CrossGap + CrossSize, 0),
            CrossColor, LineWidth);

        DrawLine(
            center + new Vector2(0, -(CrossGap + CrossSize)),
            center + new Vector2(0, -CrossGap),
            CrossColor, LineWidth);
        DrawLine(
            center + new Vector2(0, CrossGap),
            center + new Vector2(0, CrossGap + CrossSize),
            CrossColor, LineWidth);
    }
}
