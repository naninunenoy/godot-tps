using Shouldly;
using tps.csharp;

namespace tps.csharp.test;

public class CameraAimTest
{
    [Fact]
    public void CalcYawDelta_PositiveMouseX_ReturnsNegativeDelta()
    {
        CameraAim.CalcYawDelta(10f, 0.003f).ShouldBeLessThan(0f);
    }

    [Fact]
    public void CalcYawDelta_ScalesBySensitivity()
    {
        CameraAim.CalcYawDelta(100f, 0.003f).ShouldBe(-0.3f, 0.0001f);
    }

    [Fact]
    public void ClampPitch_DownwardMouse_DecreasedPitch()
    {
        var result = CameraAim.ClampPitch(0f, 100f, 0.003f, -1.2f, 0.8f);
        result.ShouldBeLessThan(0f);
    }

    [Fact]
    public void ClampPitch_ExceedsMax_ClampsToMax()
    {
        var result = CameraAim.ClampPitch(0.79f, -1000f, 0.003f, -1.2f, 0.8f);
        result.ShouldBe(0.8f);
    }

    [Fact]
    public void ClampPitch_BelowMin_ClampsToMin()
    {
        var result = CameraAim.ClampPitch(-1.19f, 1000f, 0.003f, -1.2f, 0.8f);
        result.ShouldBe(-1.2f);
    }

    [Fact]
    public void ClampPitch_WithinRange_IsNotClamped()
    {
        var result = CameraAim.ClampPitch(0f, 50f, 0.003f, -1.2f, 0.8f);
        result.ShouldBeInRange(-1.2f, 0.8f);
        result.ShouldBe(-0.15f, 0.0001f);
    }
}
