using System.Numerics;
using Shouldly;
using tps.csharp;

namespace tps.csharp.test;

public class PlayerMovementTest
{
    // カメラが -Z 前方、+X 右向き（標準 3D 座標系）
    private static readonly Vector3 Forward = new(0, 0, -1);
    private static readonly Vector3 Right = new(1, 0, 0);

    private static Vector3 Calc(
        Vector2 input,
        Vector3 vel = default,
        bool onFloor = true,
        bool jump = false,
        float speed = 5f,
        float jumpVel = 5f,
        float gravity = 9.8f,
        float delta = 0f)
        => PlayerMovement.CalcVelocity(input, Forward, Right, vel, onFloor, jump, speed, jumpVel, gravity, delta);

    [Fact]
    public void IdleOnFloorDeceleratesHorizontalVelocity()
    {
        var result = Calc(Vector2.Zero, vel: new Vector3(5, 0, 3), delta: 1f);
        result.X.ShouldBe(0f);
        result.Z.ShouldBe(0f);
    }

    [Fact]
    public void MovingForwardAppliesSpeedInNegativeZ()
    {
        // ui_up → inputDir.Y = -1 → camForward * -(-1) = Forward
        var result = Calc(new Vector2(0, -1));
        result.X.ShouldBe(0f, 0.001f);
        result.Z.ShouldBe(-5f, 0.001f);
    }

    [Fact]
    public void StrafeRightAppliesSpeedInPositiveX()
    {
        var result = Calc(new Vector2(1, 0));
        result.X.ShouldBe(5f, 0.001f);
        result.Z.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void DiagonalInputIsNormalized()
    {
        var result = Calc(new Vector2(1, -1));
        var horizontal = MathF.Sqrt(result.X * result.X + result.Z * result.Z);
        horizontal.ShouldBe(5f, 0.01f);
    }

    [Fact]
    public void JumpOnFloorSetsYVelocity()
    {
        var result = Calc(Vector2.Zero, onFloor: true, jump: true);
        result.Y.ShouldBe(5f);
    }

    [Fact]
    public void JumpNotAppliedWhenAirborne()
    {
        var result = Calc(Vector2.Zero, onFloor: false, jump: true, delta: 0f);
        result.Y.ShouldBe(0f);
    }

    [Fact]
    public void GravityAppliedWhenAirborne()
    {
        var result = Calc(Vector2.Zero, onFloor: false, gravity: 9.8f, delta: 1f);
        result.Y.ShouldBe(-9.8f, 0.001f);
    }

    [Fact]
    public void GravityNotAppliedWhenOnFloor()
    {
        var result = Calc(Vector2.Zero, onFloor: true, gravity: 9.8f, delta: 1f);
        result.Y.ShouldBe(0f);
    }
}
