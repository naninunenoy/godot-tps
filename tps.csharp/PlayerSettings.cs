namespace tps.csharp;

public sealed record PlayerSettings
{
    public float Speed { get; init; } = 5f;
    public float JumpVelocity { get; init; } = 5f;
    public float Gravity { get; init; } = 9.8f;
    public float MouseSensitivity { get; init; } = 0.003f;
    public float CameraPitchMin { get; init; } = -1.2f;
    public float CameraPitchMax { get; init; } = 0.8f;
    public float BodyRotationSpeed { get; init; } = 10f;
}
