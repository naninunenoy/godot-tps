using Shouldly;
using tps.contract;
using tps.csharp;
using VitalRouter;

namespace tps.csharp.test;

public class HealthSystemTest
{
    private static (World world, HealthSystem system, EntityId id) Setup(int maxHp = 100)
    {
        var world = new World();
        var router = new Router();
        var system = new HealthSystem(world, router);
        var id = new EntityId("target#1");
        world.Register(id);
        world.SetComponent(id, new HealthComponent(maxHp, maxHp));
        return (world, system, id);
    }

    [Fact]
    public void TakeDamageReducesHp()
    {
        var (world, system, id) = Setup(100);
        system.TakeDamage(id, 30);
        world.GetComponent<HealthComponent>(id)!.Hp.ShouldBe(70);
    }

    [Fact]
    public void TakeDamageClampsToZero()
    {
        var (world, system, id) = Setup(10);
        system.TakeDamage(id, 99);
        world.GetComponent<HealthComponent>(id)!.Hp.ShouldBe(0);
    }

    [Fact]
    public void TakeDamageOnDeadEntityReturnsFalse()
    {
        var (world, system, id) = Setup(10);
        system.TakeDamage(id, 10);
        var result = system.TakeDamage(id, 1);
        result.ShouldBeFalse();
        world.GetComponent<HealthComponent>(id)!.Hp.ShouldBe(0);
    }

    [Fact]
    public async Task TakeDamageToZeroPublishesTargetDestroyed()
    {
        var world = new World();
        var router = new Router();
        var system = new HealthSystem(world, router);
        var id = new EntityId("target#1");
        world.Register(id);
        world.SetComponent(id, new HealthComponent(10, 10));

        string? destroyedName = null;
        router.Subscribe<TargetDestroyedCommand>((cmd, _) => destroyedName = cmd.TargetName);

        system.TakeDamage(id, 10);
        await Task.Yield();

        destroyedName.ShouldBe("target#1");
    }

    [Fact]
    public void ResetRestoresMaxHp()
    {
        var (world, system, id) = Setup(50);
        system.TakeDamage(id, 40);
        system.Reset(id);
        var hp = world.GetComponent<HealthComponent>(id)!;
        hp.Hp.ShouldBe(50);
        hp.IsAlive.ShouldBeTrue();
    }

    [Fact]
    public void TakeDamage_LogsTargetHit()
    {
        var world = new World();
        var router = new Router();
        var logStore = new InMemoryLogStore();
        var system = new HealthSystem(world, router, logStore);
        var id = new EntityId("target#1");
        world.Register(id);
        world.SetComponent(id, new HealthComponent(100, 100));

        system.TakeDamage(id, 30);

        logStore.HasEvent(GameEvents.TargetHit, p => (string?)p["EntityId"] == "target#1").ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void TakeDamageToZero_LogsTargetDestroyed()
    {
        var world = new World();
        var router = new Router();
        var logStore = new InMemoryLogStore();
        var system = new HealthSystem(world, router, logStore);
        var id = new EntityId("target#1");
        world.Register(id);
        world.SetComponent(id, new HealthComponent(10, 10));

        system.TakeDamage(id, 10);

        logStore.HasEvent(GameEvents.TargetDestroyed).ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }
}
