using Shouldly;

namespace tps.csharp.test;

public class CameraAimTest
{
    /// <summary>
    /// マウスX正方向（右）の入力でYaw回転量が負値（左回転）になること。
    /// </summary>
    [Fact]
    public void CalcYawDelta_PositiveMouseX_ReturnsNegativeDelta()
    {
        CameraAim.CalcYawDelta(10f, 0.003f).ShouldBeLessThan(0f);
    }

    /// <summary>
    /// YawDeltaがマウスX入力×感度で正確にスケールされること。期待値: 100f × 0.003f = -0.3f。
    /// </summary>
    [Fact]
    public void CalcYawDelta_ScalesBySensitivity()
    {
        CameraAim.CalcYawDelta(100f, 0.003f).ShouldBe(-0.3f, 0.0001f);
    }

    /// <summary>
    /// 下方向マウス入力でピッチが負方向（下向き）に変化すること。
    /// </summary>
    [Fact]
    public void ClampPitch_DownwardMouse_DecreasedPitch()
    {
        var result = CameraAim.ClampPitch(0f, 100f, 0.003f, -1.2f, 0.8f);
        result.ShouldBeLessThan(0f);
    }

    /// <summary>
    /// ピッチが上限(0.8f)を超える入力のとき、最大値0.8fにクランプされること。
    /// </summary>
    [Fact]
    public void ClampPitch_ExceedsMax_ClampsToMax()
    {
        var result = CameraAim.ClampPitch(0.79f, -1000f, 0.003f, -1.2f, 0.8f);
        result.ShouldBe(0.8f);
    }

    /// <summary>
    /// ピッチが下限(-1.2f)を超える入力のとき、最小値-1.2fにクランプされること。
    /// </summary>
    [Fact]
    public void ClampPitch_BelowMin_ClampsToMin()
    {
        var result = CameraAim.ClampPitch(-1.19f, 1000f, 0.003f, -1.2f, 0.8f);
        result.ShouldBe(-1.2f);
    }

    /// <summary>
    /// クランプ範囲内の入力ではクランプされず、正確な値(-0.15f)が返ること。
    /// </summary>
    [Fact]
    public void ClampPitch_WithinRange_IsNotClamped()
    {
        var result = CameraAim.ClampPitch(0f, 50f, 0.003f, -1.2f, 0.8f);
        result.ShouldBeInRange(-1.2f, 0.8f);
        result.ShouldBe(-0.15f, 0.0001f);
    }
}
