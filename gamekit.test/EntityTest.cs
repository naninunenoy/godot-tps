using Shouldly;

namespace gamekit.test;

public class EntityTest
{
    private static (Entity entity, World world) CreateEntity(string name = "player")
    {
        var world = new World();
        var id = new EntityId(name);
        world.Register(id);
        return (new Entity(id, name, world), world);
    }

    /// <summary>Set() で設定した CounterComponent(10) が Get() でそのまま取得でき、World 側にも反映されること。</summary>
    [Fact]
    public void Set_StoresComponentInWorld()
    {
        var (entity, world) = CreateEntity();

        entity.Set(new CounterComponent(10));

        entity.Get<CounterComponent>().ShouldBe(new CounterComponent(10));
        world.GetComponent<CounterComponent>(entity.Id).ShouldBe(new CounterComponent(10));
    }

    /// <summary>Has() は設定済みの型に true、未設定の型に false を返すこと。</summary>
    [Fact]
    public void Has_ReflectsComponentPresence()
    {
        var (entity, _) = CreateEntity();
        entity.Set(new CounterComponent(1));

        entity.Has<CounterComponent>().ShouldBeTrue();
        entity.Has<TagComponent>().ShouldBeFalse();
    }

    /// <summary>Snapshot() の Id と Name が元の Entity と一致し、GetComponent() で設定値 (Value=5) が取得できること。</summary>
    [Fact]
    public void Snapshot_ExposesIdNameAndComponents()
    {
        var (entity, _) = CreateEntity("target#1");
        entity.Set(new CounterComponent(5));

        var snapshot = entity.Snapshot();

        snapshot.Id.ShouldBe(entity.Id);
        snapshot.Name.ShouldBe("target#1");
        snapshot.GetComponent<CounterComponent>()!.Value.ShouldBe(5);
    }
}
