namespace tps.csharp;

public interface ISceneQuery
{
    ulong FrameCount { get; }
    int ObjectCount { get; }
    IReadOnlyList<IObjectSnapshot> Snapshot { get; }
}

public interface IObjectSnapshot
{
    EntityId Id { get; }
    string Name { get; }
    T? GetComponent<T>() where T : IComponent;
}
