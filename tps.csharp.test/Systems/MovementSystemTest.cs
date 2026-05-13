using System.Numerics;
using Shouldly;

namespace tps.csharp.test;

public class MovementSystemTest
{
    private static (World world, MovementSystem system, EntityId id) Setup()
    {
        var world = new World();
        var system = new MovementSystem(world);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new TransformComponent(Vector3.Zero, Vector3.Zero));
        return (world, system, id);
    }

    private static readonly PlayerSettings Settings = new();

    /// <summary>
    /// 前方入力でUpdateを呼ぶとTransformComponentのZ速度が正値（前方向）になること。
    /// </summary>
    [Fact]
    public void UpdateChangesVelocityWithForwardInput()
    {
        var (world, system, id) = Setup();
        system.Update(
            id,
            inputDir: new Vector2(0, -1),
            camForward: Vector3.UnitZ,
            camRight: Vector3.UnitX,
            isOnFloor: true,
            jumpPressed: false,
            Settings,
            delta: 0.016f
        );

        var vel = world.GetComponent<TransformComponent>(id)!.Velocity;
        vel.Z.ShouldBeGreaterThan(0f);
    }

    /// <summary>
    /// 空中(isOnFloor=false)・delta=1fでUpdateを呼ぶとTransformComponentのY速度が負値（重力）になること。
    /// </summary>
    [Fact]
    public void UpdateAppliesGravityWhenAirborne()
    {
        var (world, system, id) = Setup();
        system.Update(
            id,
            inputDir: Vector2.Zero,
            camForward: Vector3.UnitZ,
            camRight: Vector3.UnitX,
            isOnFloor: false,
            jumpPressed: false,
            Settings,
            delta: 1f
        );

        var vel = world.GetComponent<TransformComponent>(id)!.Velocity;
        vel.Y.ShouldBeLessThan(0f);
    }

    /// <summary>
    /// FeedbackTransformを呼ぶとTransformComponentのPositionとVelocityが指定値で更新されること。
    /// </summary>
    [Fact]
    public void FeedbackTransformUpdatesPositionAndVelocity()
    {
        var (world, system, id) = Setup();
        var pos = new Vector3(1f, 0f, 2f);
        var vel = new Vector3(0f, 0f, 5f);
        system.FeedbackTransform(id, pos, vel);

        var transform = world.GetComponent<TransformComponent>(id)!;
        transform.Position.ShouldBe(pos);
        transform.Velocity.ShouldBe(vel);
    }
}
