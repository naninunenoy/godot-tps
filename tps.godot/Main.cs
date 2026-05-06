using System.Linq;
using Godot;
using tps.csharp;

namespace tps;

public partial class Main : Node3D
{
    private readonly KillCounter _killCounter = new();

    public override void _Ready()
    {
        GD.Print("main ready");
        var hud = GetNode<Hud>("HudLayer/Hud");
        hud.SetKillCounter(_killCounter);

        foreach (var target in GetChildren().OfType<Target>())
        {
            target.Destroyed += _killCounter.Increment;
        }
    }

    public override void _ExitTree()
    {
        _killCounter.Dispose();
    }

    public override void _Process(double delta)
    { }
}
