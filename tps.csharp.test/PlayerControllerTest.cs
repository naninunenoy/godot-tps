using System.Numerics;
using Shouldly;
using tps.contract;
using VitalRouter;

namespace tps.csharp.test;

public class PlayerControllerTest
{
    private static readonly Vector3 Forward = new(0, 0, -1);
    private static readonly Vector3 Right = new(1, 0, 0);
    private static readonly PlayerSettings DefaultSettings = new();

    [Fact]
    public void CalcMovement_ForwardInput_ReturnsNegativeZ()
    {
        var router = new Router();
        var ctrl = new PlayerController(router, DefaultSettings);

        var vel = ctrl.CalcMovement(new Vector2(0, -1), Forward, Right, true, false, 0f);

        vel.Z.ShouldBe(-DefaultSettings.Speed, 0.001f);
    }

    [Fact]
    public void CalcMovement_PublishesPlayerMoveCommand()
    {
        var router = new Router();
        var ctrl = new PlayerController(router, DefaultSettings);
        Vector3 received = default;
        router.Subscribe<PlayerMoveCommand>((cmd, _) => received = cmd.Velocity);

        ctrl.CalcMovement(new Vector2(0, -1), Forward, Right, true, false, 0f);

        received.Z.ShouldBe(-DefaultSettings.Speed, 0.001f);
    }

    [Fact]
    public void FeedbackVelocity_AffectsNextCalcMovement()
    {
        var router = new Router();
        var ctrl = new PlayerController(router, DefaultSettings);

        // 外部から速度をフィードバック（コリジョンで止まった想定）
        ctrl.FeedbackVelocity(Vector3.Zero);
        var vel = ctrl.CalcMovement(Vector2.Zero, Forward, Right, true, false, 0f);

        vel.X.ShouldBe(0f);
        vel.Z.ShouldBe(0f);
    }

    [Fact]
    public void CalcCameraAim_MouseDeltaX_ReturnsCorrectYawDelta()
    {
        var router = new Router();
        var ctrl = new PlayerController(router, DefaultSettings);

        var (yawDelta, _) = ctrl.CalcCameraAim(100f, 0f);

        yawDelta.ShouldBe(-100f * DefaultSettings.MouseSensitivity, 0.0001f);
    }

    [Fact]
    public void CalcCameraAim_MouseDeltaY_ClampsPitchToMax()
    {
        var router = new Router();
        var ctrl = new PlayerController(router, DefaultSettings);

        // 強く上を向く（pitch が max に張り付くまで繰り返す）
        for (var i = 0; i < 1000; i++)
            ctrl.CalcCameraAim(0f, -1000f);

        var (_, pitch) = ctrl.CalcCameraAim(0f, -1000f);
        pitch.ShouldBe(DefaultSettings.CameraPitchMax);
    }

    [Fact]
    public void CalcCameraAim_PublishesCameraOrientCommand()
    {
        var router = new Router();
        var ctrl = new PlayerController(router, DefaultSettings);
        float receivedYaw = 0f;
        float receivedPitch = 0f;
        router.Subscribe<CameraOrientCommand>(
            (cmd, _) =>
            {
                receivedYaw = cmd.YawDelta;
                receivedPitch = cmd.Pitch;
            }
        );

        ctrl.CalcCameraAim(50f, 0f);

        receivedYaw.ShouldBe(-50f * DefaultSettings.MouseSensitivity, 0.0001f);
        receivedPitch.ShouldBe(0f);
    }

    [Fact]
    public void Settings_ReflectsInjectedValues()
    {
        var router = new Router();
        var settings = new PlayerSettings { Speed = 10f, MouseSensitivity = 0.005f };
        var ctrl = new PlayerController(router, settings);

        ctrl.Settings.Speed.ShouldBe(10f);
        ctrl.Settings.MouseSensitivity.ShouldBe(0.005f);
    }
}
