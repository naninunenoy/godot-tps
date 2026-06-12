using gamekit.contract.Mcp;
using gamekit.godot;
using Godot;
using tps;
using tps.contract.Mcp;
using tps.csharp;

public partial class InputServer : Node
{
    private GameHttpServer? _server;
    private ISceneQuery? _sceneQuery;
    private IScene? _scene;
    private Player? _player;

    public void Initialize(ISceneQuery sceneQuery, IScene scene, Player player)
    {
        _sceneQuery = sceneQuery;
        _scene = scene;
        _player = player;
    }

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
            return;

        var server = new GameHttpServer(GetTree());
        GameApiRoutes.Register(server, () => _scene, () => _sceneQuery, GameStateResponseBuilder.Build);
        server.MapPostJson<SetCameraPitchRequest>(TpsEndpoints.CameraPitch, HandleCameraPitch);
        server.MapPostJson<LookAtPositionRequest>(TpsEndpoints.LookAt, HandleLookAt);
        server.MapPostJson<SetAimingRequest>(TpsEndpoints.SetAiming, HandleSetAiming);

        var err = server.Listen(InputEndpoints.Port);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[InputServer] Failed to listen on port {InputEndpoints.Port}: {err}");
            return;
        }
        GD.Print($"[InputServer] Listening on port {InputEndpoints.Port}");
        _server = server;
    }

    public override void _ExitTree() => _server?.Stop();

    public override void _Process(double delta) => _server?.Poll();

    private HttpResult HandleCameraPitch(SetCameraPitchRequest cmd)
    {
        if (_player is null)
            return HttpResult.Text("not initialized", 503);
        _player.SetCameraPitch(cmd.PitchDegrees * Mathf.Pi / 180f);
        return HttpResult.Json(new CameraControlResponse(true, $"pitch={cmd.PitchDegrees}°"));
    }

    private HttpResult HandleLookAt(LookAtPositionRequest cmd)
    {
        if (_player is null)
            return HttpResult.Text("not initialized", 503);
        _player.FaceToward(cmd.X, cmd.Y, cmd.Z);
        return HttpResult.Json(new CameraControlResponse(true, $"looking at ({cmd.X},{cmd.Y},{cmd.Z})"));
    }

    private HttpResult HandleSetAiming(SetAimingRequest cmd)
    {
        if (_player is null)
            return HttpResult.Text("not initialized", 503);
        _player.SetAiming(cmd.IsAiming);
        return HttpResult.Json(new CameraControlResponse(true, $"aiming={cmd.IsAiming}"));
    }
}
