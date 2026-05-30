using System;
using Godot;
using Microsoft.Extensions.Logging;
using tps.contract.GameCommand;
using tps.contract.Mcp;
using tps.csharp;
using tps.Logging;
using VitalRouter;

namespace tps;

[Routes]
public partial class Player : CharacterBody3D
{
    [Export]
    public PackedScene BulletScene = null!;

    [Export]
    public bool ShowRaycastDebug = true;

    private readonly ILogger<Player> _logger = AppLogger.For<Player>();

    private Entity _entity = null!;
    private WeaponSystem _weaponSystem = null!;
    private MovementSystem _movementSystem = null!;
    private Router _router = null!;
    private PlayerController _controller = null!;
    private IDisposable? _subscription;

    private Node3D _cameraPivot = null!;
    private SpringArm3D _springArm = null!;
    private MeshInstance3D _body = null!;
    private Camera3D _camera = null!;
    private MeshInstance3D? _aimMarker;
    private bool _lastIsOnTarget;

    public override void _Ready()
    {
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _springArm = GetNode<SpringArm3D>("CameraPivot/SpringArm3D");
        _body = GetNode<MeshInstance3D>("Body");
        _camera = GetNode<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * 2.5f;
        _springArm.AddExcludedObject(GetRid());

        if (ShowRaycastDebug)
        {
            _aimMarker = new MeshInstance3D
            {
                TopLevel = true,
                Mesh = new SphereMesh { Radius = 0.12f, Height = 0.24f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(1, 1, 0),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    NoDepthTest = true,
                },
            };
            AddChild(_aimMarker);
        }
    }

    public void Initialize(
        Entity entity,
        WeaponSystem weaponSystem,
        MovementSystem movementSystem,
        Router router,
        PlayerSettings settings
    )
    {
        _entity = entity;
        _weaponSystem = weaponSystem;
        _movementSystem = movementSystem;
        _router = router;
        _controller = new PlayerController(router, settings);
        _subscription = this.MapTo(_router);
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _logger.LogInformation("Player ready (IsDebugBuild={IsDebug})", OS.IsDebugBuild());

        var weapon = _entity.Get<WeaponComponent>();
        if (weapon is not null)
            _ = _router.PublishAsync(new AmmoChangedCommand { CurrentAmmo = weapon.CurrentAmmo, MagazineSize = weapon.MagazineSize, IsReloading = false });
        _ = _router.PublishAsync(new AdsStateChangedCommand { IsAiming = false });
    }

    public override void _ExitTree()
    {
        _subscription?.Dispose();
        _controller?.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (_controller is null)
            return;

        if (@event is InputEventMouseMotion motion)
        {
            var (yawDelta, pitch) = _controller.CalcCameraAim(motion.Relative.X, motion.Relative.Y);
            _cameraPivot.RotateY(yawDelta);
            var rot = _springArm.Rotation;
            rot.X = pitch;
            _springArm.Rotation = rot;
        }
        if (@event.IsActionPressed("reload"))
        {
            if (_weaponSystem.TryStartReload(_entity.Id))
            {
                var w = _entity.Get<WeaponComponent>();
                _logger.LogDebug("Reload started ammo={Ammo}/{Max}", w?.CurrentAmmo, w?.MagazineSize);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_entity is null)
            return;

        _weaponSystem.Update(_entity.Id, (float)delta);

        var weapon = _entity.Get<WeaponComponent>();
        if (weapon?.NeedsReload == true)
            _weaponSystem.TryStartReload(_entity.Id);

        _weaponSystem.SetAiming(_entity.Id, Input.IsActionPressed("aim"));

        if (Input.IsActionPressed("fire"))
            TryFire();

        UpdateAimFeedback();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_entity is null)
            return;

        float dt = (float)delta;
        _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * 2.5f;

        var camFwdGodot = -_cameraPivot.GlobalBasis.Z;
        camFwdGodot.Y = 0;
        if (!camFwdGodot.IsZeroApprox())
            camFwdGodot = camFwdGodot.Normalized();

        _entity.Set(new CameraComponent(camFwdGodot.ToNumerics()));

        Vector2 inputDir2D = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        bool jumpPressed = Input.IsActionJustPressed("jump");
        if (IsOnFloor() && jumpPressed)
            _logger.LogDebug("Jump");

        _movementSystem.Move(
            _entity.Id,
            new SN.Vector2(inputDir2D.X, inputDir2D.Y),
            IsOnFloor(),
            jumpPressed,
            _controller.Settings,
            dt
        );

        var transform = _entity.Get<TransformComponent>();
        var vel = transform?.Velocity ?? SN.Vector3.Zero;

        var camRightGodot = _cameraPivot.GlobalBasis.X;
        camRightGodot.Y = 0;
        if (!camRightGodot.IsZeroApprox())
            camRightGodot = camRightGodot.Normalized();

        var moveDirGodot = camFwdGodot * -inputDir2D.Y + camRightGodot * inputDir2D.X;
        if (moveDirGodot.LengthSquared() > 0.01f)
            _body.Basis = _body.Basis.Slerp(
                Basis.LookingAt(moveDirGodot.Normalized(), Vector3.Up),
                dt * _controller.Settings.BodyRotationSpeed
            );

        Velocity = vel.ToGodot();
        MoveAndSlide();

        _movementSystem.FeedbackTransform(
            _entity.Id,
            GlobalPosition.ToNumerics(),
            Velocity.ToNumerics()
        );
    }

    [Route]
    public void On(SetCameraPitchRequest cmd) =>
        SetCameraPitch(cmd.PitchDegrees * Mathf.Pi / 180f);

    [Route]
    public void On(LookAtPositionRequest cmd) =>
        FaceToward(cmd.X, cmd.Y, cmd.Z);

    public void SetCameraPitch(float pitchRadians)
    {
        var pitch = Mathf.Clamp(pitchRadians, _controller.Settings.CameraPitchMin, _controller.Settings.CameraPitchMax);
        var rot = _springArm.Rotation;
        rot.X = pitch;
        _springArm.Rotation = rot;
        _controller.SetPitch(pitch);
    }

    public void SetAiming(bool isAiming)
    {
        if (isAiming)
            Input.ActionPress("aim");
        else
            Input.ActionRelease("aim");
    }

    public void FaceToward(float worldX, float worldY, float worldZ)
    {
        var target = new Vector3(worldX, worldY, worldZ);
        var pivotPos = _cameraPivot.GlobalPosition;
        var delta = target - pivotPos;

        var yaw = Mathf.Atan2(-delta.X, -delta.Z);
        var pivotRot = _cameraPivot.Rotation;
        pivotRot.Y = yaw;
        _cameraPivot.Rotation = pivotRot;

        var horizontal = Mathf.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
        var pitch = Mathf.Atan2(delta.Y, horizontal);
        SetCameraPitch(pitch);
    }

    [Route]
    public void On(BulletSpawnRequested cmd)
    {
        if (BulletScene == null)
            return;
        var bullet = BulletScene.Instantiate<Bullet>();
        bullet.Initialize(_router, cmd.Speed, cmd.Damage);
        GetTree().CurrentScene.AddChild(bullet);

        // カメラから着弾点を求め、マズルからその点へ向ける（TPS カメラ高さ補正）
        var camOrigin = _camera.GlobalPosition;
        var camForward = -_camera.GlobalBasis.Z;
        var camEnd = camOrigin + camForward * 200f;
        var rayQuery = PhysicsRayQueryParameters3D.Create(camOrigin, camEnd);
        rayQuery.Exclude = [GetRid()];
        var rayResult = GetWorld3D().DirectSpaceState.IntersectRay(rayQuery);
        var aimPoint = rayResult.Count > 0
            ? rayResult["position"].AsVector3()
            : camEnd;

        var muzzlePos = GlobalPosition + Vector3.Up * 1.3f + camForward * 0.5f;
        var aimDir = (aimPoint - muzzlePos).Normalized();
        bullet.GlobalPosition = muzzlePos;
        bullet.LookAt(muzzlePos + aimDir);
        _logger.LogDebug("Bullet spawned aimPoint={AimPoint}", aimPoint);
    }

    private void TryFire()
    {
        var ads = _entity.Get<AdsComponent>();
        if (!_weaponSystem.TryFire(_entity.Id))
        {
            _logger.LogInformation("TryFire skipped (IsAiming={IsAiming}, CanFire={CanFire})",
                ads?.IsAiming, _entity.Get<WeaponComponent>()?.CanFire);
            return;
        }
        var w = _entity.Get<WeaponComponent>();
        _logger.LogInformation("Fire ammo={Ammo}/{Max}", w?.CurrentAmmo, w?.MagazineSize);
    }

    private void UpdateAimFeedback()
    {
        var origin = _camera.GlobalPosition;
        var direction = -_camera.GlobalBasis.Z;
        var end = origin + direction * 200f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.Exclude = [GetRid()];
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        var isOnTarget = result.Count > 0 && result["collider"].AsGodotObject() is Target;
        if (isOnTarget != _lastIsOnTarget)
        {
            _lastIsOnTarget = isOnTarget;
            _ = _router.PublishAsync(new AimUpdatedCommand { IsOnTarget = isOnTarget });
        }

        if (ShowRaycastDebug && _aimMarker != null)
        {
            _aimMarker.GlobalPosition = result.Count > 0 ? result["position"].AsVector3() : end;
            ((StandardMaterial3D)_aimMarker.MaterialOverride!).AlbedoColor = isOnTarget
                ? new Color(0, 1, 0)
                : new Color(1, 1, 0);
        }
    }
}
