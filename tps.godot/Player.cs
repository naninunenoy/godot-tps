using Godot;
using Microsoft.Extensions.Logging;
using R3;
using tps.contract;
using tps.csharp;
using tps.Logging;
using VitalRouter;
using SN = System.Numerics;

namespace tps;

public partial class Player : CharacterBody3D
{
    [Export] public int WeaponDamage = 1;
    [Export] public PackedScene BulletScene = null!;
    [Export] public bool ShowRaycastDebug = true;

    private readonly ILogger<Player> _logger = AppLogger.For<Player>();
    private readonly WeaponState _weapon = new(30, 2f, 0.1f);
    private readonly PlayerController _controller = new(GameRouter.Default, new PlayerSettings());

    Node3D _cameraPivot = null!;
    SpringArm3D _springArm = null!;
    MeshInstance3D _body = null!;
    Camera3D _camera = null!;
    MeshInstance3D? _aimMarker;

    public override void _Ready()
    {
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _springArm = GetNode<SpringArm3D>("CameraPivot/SpringArm3D");
        _body = GetNode<MeshInstance3D>("Body");
        _camera = GetNode<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * 2.5f;
        _springArm.AddExcludedObject(GetRid());
        Input.MouseMode = Input.MouseModeEnum.Captured;

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

        _logger.LogInformation("Player ready (IsDebugBuild={IsDebug})", OS.IsDebugBuild());
    }

    public override void _ExitTree()
    {
        _weapon.Dispose();
        _controller.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
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
            if (_weapon.TryStartReload())
                _logger.LogDebug("Reload started ammo={Ammo}/{Max}", _weapon.CurrentAmmo.CurrentValue, _weapon.MagazineSize);
        }
    }

    public override void _Process(double delta)
    {
        _weapon.Update((float)delta);

        if (_weapon.NeedsReload)
            _weapon.TryStartReload();

        if (Input.IsActionPressed("fire"))
            TryFire();

        if (ShowRaycastDebug && _aimMarker != null)
            UpdateAimMarker();
    }

    private void UpdateAimMarker()
    {
        var origin = _camera.GlobalPosition;
        var direction = -_camera.GlobalBasis.Z;
        var end = origin + direction * 100f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.Exclude = [GetRid()];
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        var hit = result.Count > 0;
        _aimMarker!.GlobalPosition = hit ? result["position"].AsVector3() : end;
        ((StandardMaterial3D)_aimMarker.MaterialOverride!).AlbedoColor =
            hit ? new Color(0, 1, 0) : new Color(1, 1, 0);
    }

    private void SpawnBullet()
    {
        if (BulletScene == null) return;
        var bullet = BulletScene.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(bullet);
        var forward = -_camera.GlobalBasis.Z;
        bullet.GlobalPosition = GlobalPosition + Vector3.Up * 1.3f + forward * 0.5f;
        bullet.GlobalBasis = _camera.GlobalBasis;
        _logger.LogDebug("Bullet spawned");
    }

    private void TryFire()
    {
        if (!_weapon.TryFire()) return;
        _ = GameRouter.Default.PublishAsync(new ShotFiredCommand { AmmoLeft = _weapon.CurrentAmmo.CurrentValue });
        _logger.LogDebug("Fire ammo={Ammo}/{Max}", _weapon.CurrentAmmo.CurrentValue, _weapon.MagazineSize);

        SpawnBullet();

        var origin = _camera.GlobalPosition;
        var direction = -_camera.GlobalBasis.Z;
        var end = origin + direction * 200f;

        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.Exclude = [GetRid()];

        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0) return;
        if (result["collider"].AsGodotObject() is Target target)
        {
            _ = GameRouter.Default.PublishAsync(new TargetHitCommand { TargetName = target.Name, Damage = WeaponDamage });
            _logger.LogDebug("Hit target={Name} damage={Damage}", target.Name, WeaponDamage);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * 2.5f;

        Vector2 inputDir2D = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        var camFwdGodot = -_cameraPivot.GlobalBasis.Z;
        camFwdGodot.Y = 0;
        if (!camFwdGodot.IsZeroApprox()) camFwdGodot = camFwdGodot.Normalized();
        var camRightGodot = _cameraPivot.GlobalBasis.X;
        camRightGodot.Y = 0;
        if (!camRightGodot.IsZeroApprox()) camRightGodot = camRightGodot.Normalized();

        bool jumpPressed = Input.IsActionJustPressed("jump");
        if (IsOnFloor() && jumpPressed) _logger.LogDebug("Jump");

        var newVel = _controller.CalcMovement(
            new SN.Vector2(inputDir2D.X, inputDir2D.Y),
            new SN.Vector3(camFwdGodot.X, camFwdGodot.Y, camFwdGodot.Z),
            new SN.Vector3(camRightGodot.X, camRightGodot.Y, camRightGodot.Z),
            IsOnFloor(), jumpPressed, dt);

        var moveDirGodot = camFwdGodot * -inputDir2D.Y + camRightGodot * inputDir2D.X;
        if (moveDirGodot.LengthSquared() > 0.01f)
            _body.Basis = _body.Basis.Slerp(
                Basis.LookingAt(moveDirGodot.Normalized(), Vector3.Up),
                dt * _controller.Settings.BodyRotationSpeed);

        Velocity = new Vector3(newVel.X, newVel.Y, newVel.Z);
        MoveAndSlide();

        // コリジョン解決後の実速度を C# 側にフィードバック
        _controller.FeedbackVelocity(new SN.Vector3(Velocity.X, Velocity.Y, Velocity.Z));
    }
}
