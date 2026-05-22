using SN = System.Numerics;

namespace tps.csharp;

public record BoundsComponent(SN.Vector3 Min, SN.Vector3 Max) : IComponent;
