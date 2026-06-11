using Godot;
using SN = System.Numerics;

namespace gamekit.godot;

public static class VectorExtensions
{
    public static SN.Vector3 ToNumerics(this Vector3 v) => new(v.X, v.Y, v.Z);

    public static Vector3 ToGodot(this SN.Vector3 v) => new(v.X, v.Y, v.Z);
}
