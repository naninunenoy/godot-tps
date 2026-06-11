namespace gamekit;

public sealed class World
{
    private readonly Dictionary<EntityId, Dictionary<Type, IComponent>> _store = new();

    public void Register(EntityId id)
    {
        _store[id] = new Dictionary<Type, IComponent>();
    }

    public void Unregister(EntityId id)
    {
        _store.Remove(id);
    }

    public bool IsRegistered(EntityId id) => _store.ContainsKey(id);

    public T? GetComponent<T>(EntityId id)
        where T : IComponent
    {
        if (!_store.TryGetValue(id, out var components))
            return default;
        return components.TryGetValue(typeof(T), out var c) ? (T)c : default;
    }

    public void SetComponent<T>(EntityId id, T component)
        where T : IComponent
    {
        if (!_store.TryGetValue(id, out var components))
            throw new InvalidOperationException($"Entity {id} is not registered in World");
        components[typeof(T)] = component;
    }

    public bool HasComponent<T>(EntityId id)
        where T : IComponent
    {
        if (!_store.TryGetValue(id, out var components))
            return false;
        return components.ContainsKey(typeof(T));
    }

    public IEnumerable<EntityId> GetEntitiesWithComponent<T>()
        where T : IComponent =>
        _store.Where(kv => kv.Value.ContainsKey(typeof(T))).Select(kv => kv.Key);

    public int Count => _store.Count;
}
