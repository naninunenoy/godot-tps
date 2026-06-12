using System.Linq;
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
        GameApiRoutes.Register(
            server,
            GetTree(),
            () => _scene,
            () => _sceneQuery is null ? null : BuildStateResponse()
        );
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

    private GameStateResponse BuildStateResponse()
    {
        var objects = _sceneQuery!.Snapshot.Select(obj =>
        {
            var health = obj.GetComponent<HealthComponent>();
            var weapon = obj.GetComponent<WeaponComponent>();
            var transform = obj.GetComponent<TransformComponent>();
            var camera = obj.GetComponent<CameraComponent>();
            var bounds = obj.GetComponent<BoundsComponent>();

            WeaponDto? weaponDto = null;
            if (weapon is not null)
            {
                Vec3Dto? muzzlePos = null;
                Vec3Dto? muzzleDir = null;
                if (transform is not null)
                {
                    var pos = transform.Position;
                    muzzlePos = new Vec3Dto(pos.X, pos.Y + 1.3f, pos.Z);
                }
                if (camera is not null)
                    muzzleDir = new Vec3Dto(camera.Forward.X, camera.Forward.Y, camera.Forward.Z);
                var ads = obj.GetComponent<AdsComponent>();
                weaponDto = new WeaponDto(weapon.CurrentAmmo, weapon.MagazineSize, weapon.IsReloading, muzzlePos, muzzleDir, ads?.IsAiming);
            }

            BoundsDto? boundsDto = null;
            if (bounds is not null)
                boundsDto = new BoundsDto(
                    new Vec3Dto(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
                    new Vec3Dto(bounds.Max.X, bounds.Max.Y, bounds.Max.Z)
                );

            return new ObjectSnapshotDto(
                obj.Id.AsPrimitive(),
                obj.Name,
                health is not null ? new HealthDto(health.Hp, health.MaxHp) : null,
                weaponDto,
                boundsDto
            );
        }).ToArray();
        return new GameStateResponse(_sceneQuery.FrameCount, _sceneQuery.ObjectCount, objects);
    }
}
