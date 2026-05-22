using System.Numerics;

namespace tps.csharp;

public sealed class MovementSystem(World world)
{
    public void Move(
        EntityId id,
        Vector2 inputDir,
        bool isOnFloor,
        bool jumpPressed,
        PlayerSettings settings,
        float delta
    )
    {
        var transform = world.GetComponent<TransformComponent>(id);
        if (transform is null)
            return;

        var camera = world.GetComponent<CameraComponent>(id);
        var camFwd = camera?.Forward ?? Vector3.UnitZ;

        var right = Vector3.Cross(Vector3.UnitY, camFwd);
        var camRight = right.LengthSquared() > 0.0001f ? Vector3.Normalize(right) : Vector3.UnitX;

        var newVelocity = PlayerMovement.CalcVelocity(
            inputDir,
            camFwd,
            camRight,
            transform.Velocity,
            isOnFloor,
            jumpPressed,
            settings.Speed,
            settings.JumpVelocity,
            settings.Gravity,
            delta
        );

        world.SetComponent(id, transform with { Velocity = newVelocity });
    }

    public void FeedbackTransform(EntityId id, Vector3 actualPosition, Vector3 actualVelocity)
    {
        var transform = world.GetComponent<TransformComponent>(id);
        if (transform is null)
            return;
        world.SetComponent(
            id,
            transform with
            {
                Position = actualPosition,
                Velocity = actualVelocity,
            }
        );
    }
}
