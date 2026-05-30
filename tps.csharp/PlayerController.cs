using System.Numerics;
using tps.contract.GameCommand;
using VitalRouter;

namespace tps.csharp;

public sealed class PlayerController : IDisposable
{
    private readonly Router _router;
    public PlayerSettings Settings { get; }

    private Vector3 _velocity;
    private float _cameraPitch;

    public PlayerController(Router router, PlayerSettings settings)
    {
        _router = router;
        Settings = settings;
    }

    // 毎フレーム: 入力から目標速度を計算し PlayerMoveCommand を発行して返す
    public Vector3 CalcMovement(
        Vector2 inputDir,
        Vector3 camForward,
        Vector3 camRight,
        bool isOnFloor,
        bool jumpPressed,
        float delta
    )
    {
        _velocity = PlayerMovement.CalcVelocity(
            inputDir,
            camForward,
            camRight,
            _velocity,
            isOnFloor,
            jumpPressed,
            Settings.Speed,
            Settings.JumpVelocity,
            Settings.Gravity,
            delta
        );

        _ = _router.PublishAsync(new PlayerMoveCommand { Velocity = _velocity });
        return _velocity;
    }

    // MoveAndSlide 後にコリジョン解決済みの実速度をフィードバック
    public void FeedbackVelocity(Vector3 actualVelocity) => _velocity = actualVelocity;

    // マウス入力からカメラ角度を計算し CameraOrientCommand を発行して返す
    public (float yawDelta, float pitch) CalcCameraAim(float mouseDeltaX, float mouseDeltaY)
    {
        var yawDelta = CameraAim.CalcYawDelta(mouseDeltaX, Settings.MouseSensitivity);
        _cameraPitch = CameraAim.ClampPitch(
            _cameraPitch,
            mouseDeltaY,
            Settings.MouseSensitivity,
            Settings.CameraPitchMin,
            Settings.CameraPitchMax
        );

        _ = _router.PublishAsync(
            new CameraOrientCommand { YawDelta = yawDelta, Pitch = _cameraPitch }
        );
        return (yawDelta, _cameraPitch);
    }

    public void SetPitch(float pitch)
    {
        _cameraPitch = Math.Clamp(pitch, Settings.CameraPitchMin, Settings.CameraPitchMax);
    }

    public void Dispose() { }
}
