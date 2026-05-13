using System.Numerics;

namespace tps.csharp;

public static class PlayerMovement
{
    /// <param name="camForward">Y=0 に正規化済みのカメラ前方ベクトル</param>
    /// <param name="camRight">Y=0 に正規化済みのカメラ右ベクトル</param>
    public static Vector3 CalcVelocity(
        Vector2 inputDir,
        Vector3 camForward,
        Vector3 camRight,
        Vector3 currentVelocity,
        bool isOnFloor,
        bool jumpPressed,
        float speed,
        float jumpVelocity,
        float gravity,
        float delta
    )
    {
        var moveDir = camForward * -inputDir.Y + camRight * inputDir.X;
        if (moveDir.LengthSquared() > 0.01f)
            moveDir = Vector3.Normalize(moveDir);

        var vel = currentVelocity;

        if (isOnFloor && jumpPressed)
            vel.Y = jumpVelocity;

        if (moveDir.LengthSquared() > 0.0001f)
        {
            vel.X = moveDir.X * speed;
            vel.Z = moveDir.Z * speed;
        }
        else
        {
            vel.X = MoveToward(vel.X, 0f, speed);
            vel.Z = MoveToward(vel.Z, 0f, speed);
        }

        if (!isOnFloor)
            vel.Y -= gravity * delta;

        return vel;
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        var diff = target - current;
        if (Math.Abs(diff) <= maxDelta)
            return target;
        return current + (diff > 0 ? 1f : -1f) * maxDelta;
    }
}
