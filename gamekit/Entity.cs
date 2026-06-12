namespace gamekit;

public sealed class Entity
{
    public EntityId Id { get; }
    public string Name { get; }
    private readonly World _world;

    public Entity(EntityId id, string name, World world)
    {
        Id = id;
        Name = name;
        _world = world;
    }

    public T? Get<T>()
        where T : IComponent => _world.GetComponent<T>(Id);

    public void Set<T>(T component)
        where T : IComponent => _world.SetComponent(Id, component);

    public bool Has<T>()
        where T : IComponent => _world.HasComponent<T>(Id);

    public IObjectSnapshot Snapshot() => new EntitySnapshot(this);

    private sealed class EntitySnapshot(Entity entity) : IObjectSnapshot
    {
        public EntityId Id => entity.Id;
        public string Name => entity.Name;

        public T? GetComponent<T>()
            where T : IComponent => entity.Get<T>();
    }
}
