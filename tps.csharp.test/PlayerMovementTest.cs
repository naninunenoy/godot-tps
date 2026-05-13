using System.Numerics;
using Shouldly;

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
        float delta = 0f
    ) =>
        PlayerMovement.CalcVelocity(
            input,
            Forward,
            Right,
            vel,
            onFloor,
            jump,
            speed,
            jumpVel,
            gravity,
            delta
        );

    /// <summary>
    /// 入力なし・床上・delta=1fのとき水平速度(X, Z)がゼロに減速すること。
    /// </summary>
    [Fact]
    public void IdleOnFloorDeceleratesHorizontalVelocity()
    {
        var result = Calc(Vector2.Zero, vel: new Vector3(5, 0, 3), delta: 1f);
        result.X.ShouldBe(0f);
        result.Z.ShouldBe(0f);
    }

    /// <summary>
    /// 前方入力(0,-1)でZ速度が-5f(前方向)になること。
    /// </summary>
    [Fact]
    public void MovingForwardAppliesSpeedInNegativeZ()
    {
        // ui_up → inputDir.Y = -1 → camForward * -(-1) = Forward
        var result = Calc(new Vector2(0, -1));
        result.X.ShouldBe(0f, 0.001f);
        result.Z.ShouldBe(-5f, 0.001f);
    }

    /// <summary>
    /// 右入力(1,0)でX速度が+5f(右方向)になること。
    /// </summary>
    [Fact]
    public void StrafeRightAppliesSpeedInPositiveX()
    {
        var result = Calc(new Vector2(1, 0));
        result.X.ShouldBe(5f, 0.001f);
        result.Z.ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 斜め入力(1,-1)でも水平速度の大きさがspeed(5f)に正規化されること。
    /// </summary>
    [Fact]
    public void DiagonalInputIsNormalized()
    {
        var result = Calc(new Vector2(1, -1));
        var horizontal = MathF.Sqrt(result.X * result.X + result.Z * result.Z);
        horizontal.ShouldBe(5f, 0.01f);
    }

    /// <summary>
    /// 床上でジャンプ入力するとY速度がjumpVel(5f)になること。
    /// </summary>
    [Fact]
    public void JumpOnFloorSetsYVelocity()
    {
        var result = Calc(Vector2.Zero, onFloor: true, jump: true);
        result.Y.ShouldBe(5f);
    }

    /// <summary>
    /// 空中でジャンプ入力してもY速度が変化しないこと（0fのまま）。
    /// </summary>
    [Fact]
    public void JumpNotAppliedWhenAirborne()
    {
        var result = Calc(Vector2.Zero, onFloor: false, jump: true, delta: 0f);
        result.Y.ShouldBe(0f);
    }

    /// <summary>
    /// 空中(isOnFloor=false)・delta=1fのとき重力(-9.8f)がY速度に加算されること。
    /// </summary>
    [Fact]
    public void GravityAppliedWhenAirborne()
    {
        var result = Calc(Vector2.Zero, onFloor: false, gravity: 9.8f, delta: 1f);
        result.Y.ShouldBe(-9.8f, 0.001f);
    }

    /// <summary>
    /// 床上ではY速度が0のままで重力が適用されないこと。
    /// </summary>
    [Fact]
    public void GravityNotAppliedWhenOnFloor()
    {
        var result = Calc(Vector2.Zero, onFloor: true, gravity: 9.8f, delta: 1f);
        result.Y.ShouldBe(0f);
    }
}
