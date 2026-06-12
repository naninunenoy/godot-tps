using gamekit.contract.Mcp;
using gamekit.godot;
using gamekit.godot.Logging;
using Godot;
using Microsoft.Extensions.Logging;
using tps.contract.Mcp;
using tps.csharp;
using VitalRouter;

public partial class InputServer : Node
{
    private readonly ILogger<InputServer> _logger = AppLogger.For<InputServer>();
    private GameHttpServer? _server;
    private ISceneQuery? _sceneQuery;
    private IScene? _scene;
    private Router? _router;

    public void Initialize(ISceneQuery sceneQuery, IScene scene, Router router)
    {
        _sceneQuery = sceneQuery;
        _scene = scene;
        _router = router;
    }

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
            return;

        var server = new GameHttpServer(GetTree());
        GameApiRoutes.Register(server, () => _scene, () => _sceneQuery, GameStateResponseBuilder.Build);
        // TPS 固有ルート。CQRS 規約（外部からの書き込みはコマンド経由）に従い、Router へ publish するだけ
        server.MapPostJson<SetCameraPitchRequest>(
            TpsEndpoints.CameraPitch,
            cmd => Publish(cmd, $"pitch={cmd.PitchDegrees}°")
        );
        server.MapPostJson<LookAtPositionRequest>(
            TpsEndpoints.LookAt,
            cmd => Publish(cmd, $"looking at ({cmd.X},{cmd.Y},{cmd.Z})")
        );
        server.MapPostJson<SetAimingRequest>(
            TpsEndpoints.SetAiming,
            cmd => Publish(cmd, $"aiming={cmd.IsAiming}")
        );

        var err = server.Listen(InputEndpoints.Port);
        if (err != Error.Ok)
        {
            _logger.LogError("Failed to listen on port {Port}: {Error}", InputEndpoints.Port, err);
            return;
        }
        _logger.LogInformation("Listening on port {Port}", InputEndpoints.Port);
        _server = server;
    }

    public override void _ExitTree() => _server?.Stop();

    public override void _Process(double delta) => _server?.Poll();

    private HttpResult Publish<TCommand>(TCommand command, string message)
        where TCommand : ICommand
    {
        if (_router is null)
            return HttpResult.Text("not initialized", 503);
        _ = _router.PublishAsync(command);
        return HttpResult.Json(new CameraControlResponse(true, message));
    }
}
