using Shouldly;
using tps.contract;
using tps.csharp;
using VitalRouter;

namespace tps.csharp.test;

public class WeaponSystemTest
{
    private static (World world, WeaponSystem system, EntityId id) Setup(int ammo = 30)
    {
        var world = new World();
        var router = new Router();
        var system = new WeaponSystem(world, router);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new WeaponComponent(
            Ammo: ammo, MagazineSize: 30,
            ReloadTimer: 0f, FireCooldown: 0f,
            ReloadDuration: 2f, FireInterval: 0.1f));
        return (world, system, id);
    }

    [Fact]
    public void TryFireReducesAmmo()
    {
        var (world, system, id) = Setup();
        system.TryFire(id);
        world.GetComponent<WeaponComponent>(id)!.Ammo.ShouldBe(29);
    }

    [Fact]
    public void TryFireWhenEmptyReturnsFalse()
    {
        var (world, system, id) = Setup(ammo: 0);
        system.TryFire(id).ShouldBeFalse();
    }

    [Fact]
    public void TryFireDuringReloadReturnsFalse()
    {
        var (world, system, id) = Setup(ammo: 10);
        system.TryStartReload(id);
        system.TryFire(id).ShouldBeFalse();
    }

    [Fact]
    public async Task TryFirePublishesShotFired()
    {
        var world = new World();
        var router = new Router();
        var system = new WeaponSystem(world, router);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new WeaponComponent(30, 30, 0f, 0f, 2f, 0.1f));

        int? ammoLeft = null;
        router.Subscribe<ShotFiredCommand>((cmd, _) => ammoLeft = cmd.AmmoLeft);

        system.TryFire(id);
        await Task.Yield();

        ammoLeft.ShouldBe(29);
    }

    [Fact]
    public void TryStartReloadSetsTimer()
    {
        var (world, system, id) = Setup(ammo: 10);
        system.TryStartReload(id);
        var weapon = world.GetComponent<WeaponComponent>(id)!;
        weapon.IsReloading.ShouldBeTrue();
        weapon.Ammo.ShouldBe(0);
    }

    [Fact]
    public void UpdateCompletesReloadAndRestoresAmmo()
    {
        var (world, system, id) = Setup(ammo: 10);
        system.TryStartReload(id);
        system.Update(id, 2.1f);
        var weapon = world.GetComponent<WeaponComponent>(id)!;
        weapon.IsReloading.ShouldBeFalse();
        weapon.Ammo.ShouldBe(30);
    }

    [Fact]
    public void UpdateDecrementsFireCooldown()
    {
        var (world, system, id) = Setup();
        system.TryFire(id);
        system.Update(id, 0.05f);
        world.GetComponent<WeaponComponent>(id)!.FireCooldown.ShouldBe(0.05f, tolerance: 0.001f);
    }

    [Fact]
    public void TryFire_LogsShotFired()
    {
        var world = new World();
        var router = new Router();
        var logStore = new InMemoryLogStore();
        var system = new WeaponSystem(world, router, logStore);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new WeaponComponent(30, 30, 0f, 0f, 2f, 0.1f));

        system.TryFire(id);

        logStore.HasEvent(GameEvents.ShotFired, p => (int?)p["AmmoLeft"] == 29).ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void TryStartReload_LogsReloadStarted()
    {
        var world = new World();
        var router = new Router();
        var logStore = new InMemoryLogStore();
        var system = new WeaponSystem(world, router, logStore);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new WeaponComponent(10, 30, 0f, 0f, 2f, 0.1f));

        system.TryStartReload(id);

        logStore.HasEvent(GameEvents.ReloadStarted).ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateCompletesReload_LogsReloadCompleted()
    {
        var world = new World();
        var router = new Router();
        var logStore = new InMemoryLogStore();
        var system = new WeaponSystem(world, router, logStore);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new WeaponComponent(10, 30, 0f, 0f, 2f, 0.1f));

        system.TryStartReload(id);
        system.Update(id, 2.1f);

        logStore.HasEvent(GameEvents.ReloadCompleted, p => (int?)p["Ammo"] == 30).ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }
}
