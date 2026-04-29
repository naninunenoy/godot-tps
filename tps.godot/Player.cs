using Godot;
using Microsoft.Extensions.Logging;
using tps.Logging;

namespace tps;

public partial class Player : CharacterBody3D
{
    [Export] public float Speed = 5f;
    [Export] public float MouseSensitivity = 0.003f;

    private readonly ILogger<Player> _logger = AppLogger.For<Player>();

    const float Gravity = 9.8f;

    Node3D _cameraPivot;
    SpringArm3D _springArm;
    MeshInstance3D _body;

    public override void _Ready()
    {
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _springArm = GetNode<SpringArm3D>("CameraPivot/SpringArm3D");
        _body = GetNode<MeshInstance3D>("Body");
        _cameraPivot.GlobalPosition = GlobalPosition;
        _springArm.AddExcludedObject(GetRid());
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _logger.LogInformation("Player ready (IsDebugBuild={IsDebug})", OS.IsDebugBuild());
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            _cameraPivot.RotateY(-motion.Relative.X * MouseSensitivity);
            var rot = _springArm.Rotation;
            rot.X = Mathf.Clamp(rot.X - motion.Relative.Y * MouseSensitivity, -0.2f, 1.0f);
            _springArm.Rotation = rot;
        }
        if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true })
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _cameraPivot.GlobalPosition = GlobalPosition;

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
