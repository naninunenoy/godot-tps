using System.Numerics;

namespace tps.csharp;

public sealed class MovementSystem(World world)
{
    public void Update(
        EntityId id,
        Vector2 inputDir,
        Vector3 camForward,
        Vector3 camRight,
        bool isOnFloor,
        bool jumpPressed,
        PlayerSettings settings,
        float delta)
    {
        var transform = world.GetComponent<TransformComponent>(id);
        if (transform is null) return;

        var newVelocity = PlayerMovement.CalcVelocity(
            inputDir, camForward, camRight,
            transform.Velocity,
            isOnFloor, jumpPressed,
            settings.Speed, settings.JumpVelocity, settings.Gravity,
            delta);

        world.SetComponent(id, transform with { Velocity = newVelocity });
    }

    public void FeedbackTransform(EntityId id, Vector3 actualPosition, Vector3 actualVelocity)
    {
        var transform = world.GetComponent<TransformComponent>(id);
        if (transform is null) return;
        world.SetComponent(id, transform with { Position = actualPosition, Velocity = actualVelocity });
    }
}
