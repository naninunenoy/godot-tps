namespace tps.csharp;

public static class CameraAim
{
    public static float CalcYawDelta(float mouseDeltaX, float sensitivity) =>
        -mouseDeltaX * sensitivity;

    public static float ClampPitch(
        float currentPitch,
        float mouseDeltaY,
        float sensitivity,
        float min,
        float max
    ) => Math.Clamp(currentPitch - mouseDeltaY * sensitivity, min, max);
}
