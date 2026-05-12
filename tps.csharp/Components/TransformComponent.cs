using System.Numerics;

namespace tps.csharp;

public record TransformComponent(Vector3 Position, Vector3 Velocity) : IComponent;
