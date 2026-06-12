using Shouldly;

namespace gamekit.test;

public class WorldTest
{
    /// <summary>Register() した EntityId は IsRegistered が true、未登録の EntityId は false を返すこと。</summary>
    [Fact]
    public void Register_MakesEntityRegistered()
    {
        var world = new World();
        var id = new EntityId("e#1");

        world.Register(id);

        world.IsRegistered(id).ShouldBeTrue();
        world.IsRegistered(new EntityId("e#2")).ShouldBeFalse();
    }

    /// <summary>SetComponent() で設定した CounterComponent(42) が GetComponent() でそのまま取得できること。</summary>
    [Fact]
    public void SetComponent_CanBeRetrieved()
    {
        var world = new World();
        var id = new EntityId("e#1");
        world.Register(id);

        world.SetComponent(id, new CounterComponent(42));

        world.GetComponent<CounterComponent>(id).ShouldBe(new CounterComponent(42));
    }

    /// <summary>同じ型のコンポーネントを再設定したとき、後から設定した値 (Value=2) で上書きされること。</summary>
    [Fact]
    public void SetComponent_OverwritesExistingComponent()
    {
        var world = new World();
        var id = new EntityId("e#1");
        world.Register(id);

        world.SetComponent(id, new CounterComponent(1));
        world.SetComponent(id, new CounterComponent(2));

        world.GetComponent<CounterComponent>(id)!.Value.ShouldBe(2);
    }

    /// <summary>未登録の EntityId に SetComponent() すると InvalidOperationException が投げられること。</summary>
    [Fact]
    public void SetComponent_ThrowsForUnregisteredEntity()
    {
        var world = new World();

        Should.Throw<InvalidOperationException>(() =>
            world.SetComponent(new EntityId("ghost"), new CounterComponent(1))
        );
    }

    /// <summary>未登録の EntityId の GetComponent() は null (default) を返すこと。</summary>
    [Fact]
    public void GetComponent_ReturnsNullForUnregisteredEntity()
    {
        var world = new World();

        world.GetComponent<CounterComponent>(new EntityId("ghost")).ShouldBeNull();
    }

    /// <summary>登録済みでも未設定のコンポーネント型の GetComponent() は null を返すこと。</summary>
    [Fact]
    public void GetComponent_ReturnsNullForMissingComponent()
    {
        var world = new World();
        var id = new EntityId("e#1");
        world.Register(id);

        world.GetComponent<CounterComponent>(id).ShouldBeNull();
    }

    /// <summary>HasComponent() は設定済みの型に true、未設定の型に false を返すこと。</summary>
    [Fact]
    public void HasComponent_ReflectsComponentPresence()
    {
        var world = new World();
        var id = new EntityId("e#1");
        world.Register(id);
        world.SetComponent(id, new CounterComponent(1));

        world.HasComponent<CounterComponent>(id).ShouldBeTrue();
        world.HasComponent<TagComponent>(id).ShouldBeFalse();
    }

    /// <summary>Unregister() した EntityId は IsRegistered が false になり、Count が 1 から 0 に減ること。</summary>
    [Fact]
    public void Unregister_RemovesEntity()
    {
        var world = new World();
        var id = new EntityId("e#1");
        world.Register(id);
        world.Count.ShouldBe(1);

        world.Unregister(id);

        world.IsRegistered(id).ShouldBeFalse();
        world.Count.ShouldBe(0);
    }

    /// <summary>GetEntitiesWithComponent() が、指定型のコンポーネントを持つ Entity (2件中1件) のみ返すこと。</summary>
    [Fact]
    public void GetEntitiesWithComponent_FiltersByComponentType()
    {
        var world = new World();
        var withCounter = new EntityId("e#1");
        var withTag = new EntityId("e#2");
        world.Register(withCounter);
        world.Register(withTag);
        world.SetComponent(withCounter, new CounterComponent(1));
        world.SetComponent(withTag, new TagComponent("enemy"));

        world.GetEntitiesWithComponent<CounterComponent>().ShouldBe([withCounter]);
    }

    /// <summary>Entity を3件 Register したとき Count が 3 を返すこと。</summary>
    [Fact]
    public void Count_ReturnsNumberOfRegisteredEntities()
    {
        var world = new World();
        world.Register(new EntityId("e#1"));
        world.Register(new EntityId("e#2"));
        world.Register(new EntityId("e#3"));

        world.Count.ShouldBe(3);
    }
}
