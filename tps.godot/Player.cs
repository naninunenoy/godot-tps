using Godot;
using Microsoft.Extensions.Logging;
using R3;
using tps.csharp;
using tps.Logging;

namespace tps;

public partial class Player : CharacterBody3D
{
    [Export] public float Speed = 5f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float JumpVelocity = 5f;
    [Export] public int WeaponDamage = 1;
    [Export] public PackedScene BulletScene = null!;

    private readonly ILogger<Player> _logger = AppLogger.For<Player>();

    const float Gravity = 9.8f;

    Node3D _cameraPivot = null!;
    SpringArm3D _springArm = null!;
    MeshInstance3D _body = null!;
    Camera3D _camera = null!;

    readonly WeaponState _weapon = new(30, 2f, 0.1f);
    bool _isGameMode = true;

    public override void _Ready()
    {
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _springArm = GetNode<SpringArm3D>("CameraPivot/SpringArm3D");
        _body = GetNode<MeshInstance3D>("Body");
        _camera = GetNode<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * 1.5f;
        _springArm.AddExcludedObject(GetRid());
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _logger.LogInformation("Player ready (IsDebugBuild={IsDebug})", OS.IsDebugBuild());
    }

    public override void _ExitTree()
    {
        _weapon.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            _cameraPivot.RotateY(-motion.Relative.X * MouseSensitivity);
            var rot = _springArm.Rotation;
            rot.X = Mathf.Clamp(rot.X - motion.Relative.Y * MouseSensitivity, -1.2f, 0.8f);
            _springArm.Rotation = rot;
        }
        if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            _isGameMode = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
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

        if (Input.IsActionPressed("fire") && _isGameMode)
            TryFire();
    }

    private void SpawnBullet()
    {
        if (BulletScene == null) return;
        var bullet = BulletScene.Instantiate<Node3D>();
        GetTree().CurrentScene.AddChild(bullet);
        var forward = -_camera.GlobalBasis.Z;
        bullet.GlobalPosition = _cameraPivot.GlobalPosition + forward * 0.5f;
        bullet.GlobalBasis = _camera.GlobalBasis;
        _logger.LogDebug("Bullet spawned");
    }

    private void TryFire()
    {
        if (!_weapon.TryFire()) return;
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
            target.TakeDamage(WeaponDamage);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * 1.5f;

        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        Vector3 camForward = -_cameraPivot.GlobalBasis.Z;
        camForward.Y = 0;
        if (!camForward.IsZeroApprox()) camForward = camForward.Normalized();

        Vector3 camRight = _cameraPivot.GlobalBasis.X;
        camRight.Y = 0;
        if (!camRight.IsZeroApprox()) camRight = camRight.Normalized();

        Vector3 moveDir = camForward * -inputDir.Y + camRight * inputDir.X;
        if (moveDir.LengthSquared() > 0.01f)
            moveDir = moveDir.Normalized();

        var vel = Velocity;

        if (IsOnFloor() && Input.IsActionJustPressed("jump"))
        {
            vel.Y = JumpVelocity;
            _logger.LogDebug("Jump");
        }

        if (moveDir != Vector3.Zero)
        {
            vel.X = moveDir.X * Speed;
            vel.Z = moveDir.Z * Speed;
            _body.Basis = _body.Basis.Slerp(Basis.LookingAt(moveDir, Vector3.Up), dt * 10f);
            _logger.LogDebug("move dir={Dir} vel=({X:F2}, {Z:F2})", moveDir, vel.X, vel.Z);
        }
        else
        {
            vel.X = Mathf.MoveToward(vel.X, 0f, Speed);
            vel.Z = Mathf.MoveToward(vel.Z, 0f, Speed);
        }

        if (!IsOnFloor())
            vel.Y -= Gravity * dt;

        Velocity = vel;
        MoveAndSlide();
    }
}
