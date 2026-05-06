using Godot;
using Microsoft.Extensions.Logging;
using R3;
using tps.csharp;
using tps.Logging;

namespace tps;

public partial class Hud : Control
{
	[Export] public float CrossSize = 14f;
	[Export] public float CrossGap  =  4f;
	[Export] public float LineWidth =  2f;
	[Export] public Color CrossColor = new(1f, 1f, 1f, 0.85f);

	private Label _killCountLabel = null!;
	private readonly CompositeDisposable _disposables = new();
	private readonly ILogger<Hud> _logger = AppLogger.For<Hud>();

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		_killCountLabel = GetNode<Label>("KillCountLabel");
		_killCountLabel.Text = "Kills: 0";
		_logger.LogDebug("Hud ready");
	}

	public override void _ExitTree()
	{
		_disposables.Dispose();
	}

	public void SetKillCounter(KillCounter counter)
	{
		counter.Count.Subscribe(count =>
		{
			_killCountLabel.Text = $"Kills: {count}";
			_logger.LogDebug("Kill count updated: {Count}", count);
		}).AddTo(_disposables);
	}

	public override void _Draw()
	{
		var center = Size / 2f;

		// 水平線: 左腕 / 右腕
		DrawLine(
			center + new Vector2(-(CrossGap + CrossSize), 0),
			center + new Vector2(-CrossGap, 0),
			CrossColor, LineWidth);
		DrawLine(
			center + new Vector2(CrossGap, 0),
			center + new Vector2(CrossGap + CrossSize, 0),
			CrossColor, LineWidth);

		// 垂直線: 上腕 / 下腕
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
