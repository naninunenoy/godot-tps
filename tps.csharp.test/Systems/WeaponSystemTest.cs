using Shouldly;
using tps.contract;
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
        world.SetComponent(
            id,
            new WeaponComponent(
                CurrentAmmo: ammo,
                MagazineSize: 30,
                ReloadTimer: 0f,
                FireCooldown: 0f,
                ReloadDuration: 2f,
                FireInterval: 0.1f
            )
        );
        world.SetComponent(id, new AdsComponent(IsAiming: true));
        world.SetComponent(id, new CameraComponent(System.Numerics.Vector3.UnitZ));
        return (world, system, id);
    }

    /// <summary>
    /// TryFire()で弾薬が1減ること（30→29）。
    /// </summary>
    [Fact]
    public void TryFireReducesAmmo()
    {
        var (world, system, id) = Setup();
        system.TryFire(id);
        world.GetComponent<WeaponComponent>(id)!.CurrentAmmo.ShouldBe(29);
    }

    /// <summary>
    /// 弾薬0のときTryFire()がfalseを返すこと。
    /// </summary>
    [Fact]
    public void TryFireWhenEmptyReturnsFalse()
    {
        var (world, system, id) = Setup(ammo: 0);
        system.TryFire(id).ShouldBeFalse();
    }

    /// <summary>
    /// リロード中はTryFire()がfalseを返すこと。
    /// </summary>
    [Fact]
    public void TryFireDuringReloadReturnsFalse()
    {
        var (world, system, id) = Setup(ammo: 10);
        system.TryStartReload(id);
        system.TryFire(id).ShouldBeFalse();
    }

    /// <summary>
    /// ADS中でないときTryFire()がfalseを返すこと。
    /// </summary>
    [Fact]
    public void TryFireWithoutAdsReturnsFalse()
    {
        var (world, system, id) = Setup();
        world.SetComponent(id, new AdsComponent(IsAiming: false));
        system.TryFire(id).ShouldBeFalse();
    }

    /// <summary>
    /// TryFire()後にShotFiredCommandがAmmoLeft=29でpublishされること。
    /// </summary>
    [Fact]
    public async Task TryFirePublishesShotFired()
    {
        var world = new World();
        var router = new Router();
        var system = new WeaponSystem(world, router);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new WeaponComponent(30, 30, 0f, 0f, 2f, 0.1f));
        world.SetComponent(id, new AdsComponent(IsAiming: true));
        world.SetComponent(id, new CameraComponent(System.Numerics.Vector3.UnitZ));

        int? ammoLeft = null;
        router.Subscribe<ShotFiredCommand>((cmd, _) => ammoLeft = cmd.AmmoLeft);

        system.TryFire(id);
        await Task.Yield();

        ammoLeft.ShouldBe(29);
    }

    /// <summary>
    /// TryStartReload()でIsReloadingがtrueになり、弾薬がマガジンから消費されて0になること。
    /// </summary>
    [Fact]
    public void TryStartReloadSetsTimer()
    {
        var (world, system, id) = Setup(ammo: 10);
        system.TryStartReload(id);
        var weapon = world.GetComponent<WeaponComponent>(id)!;
        weapon.IsReloading.ShouldBeTrue();
        weapon.CurrentAmmo.ShouldBe(0);
    }

    /// <summary>
    /// リロード時間(2f)を超えてUpdate(2.1f)を呼ぶと弾薬がマガジン最大(30)に戻り、IsReloadingがfalseになること。
    /// </summary>
    [Fact]
    public void UpdateCompletesReloadAndRestoresAmmo()
    {
        var (world, system, id) = Setup(ammo: 10);
        system.TryStartReload(id);
        system.Update(id, 2.1f);
        var weapon = world.GetComponent<WeaponComponent>(id)!;
        weapon.IsReloading.ShouldBeFalse();
        weapon.CurrentAmmo.ShouldBe(30);
    }

    /// <summary>
    /// 発射後にUpdate(0.05f)を呼ぶとFireCooldownが0.05f減算されること。
    /// </summary>
    [Fact]
    public void UpdateDecrementsFireCooldown()
    {
        var (world, system, id) = Setup();
        system.TryFire(id);
        system.Update(id, 0.05f);
        world.GetComponent<WeaponComponent>(id)!.FireCooldown.ShouldBe(0.05f, tolerance: 0.001f);
    }

    /// <summary>
    /// TryFire()後にShotFiredイベントがAmmoLeft=29でログに記録され、エラーがないこと。
    /// </summary>
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
        world.SetComponent(id, new AdsComponent(IsAiming: true));
        world.SetComponent(id, new CameraComponent(System.Numerics.Vector3.UnitZ));

        system.TryFire(id);

        logStore.HasEvent(GameEvents.ShotFired, p => (int?)p["AmmoLeft"] == 29).ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// TryStartReload()後にReloadStartedイベントがログに記録され、エラーがないこと。
    /// </summary>
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

    /// <summary>
    /// リロード完了後にReloadCompletedイベントがAmmo=30でログに記録され、エラーがないこと。
    /// </summary>
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

    /// <summary>
    /// SetAiming(true)でAdsComponent.IsAimingがtrueになること。
    /// </summary>
    [Fact]
    public void SetAimingTrueSetsIsAimingTrue()
    {
        var world = new World();
        var router = new Router();
        var system = new WeaponSystem(world, router);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new AdsComponent(IsAiming: false));

        system.SetAiming(id, true);

        world.GetComponent<AdsComponent>(id)!.IsAiming.ShouldBeTrue();
    }

    /// <summary>
    /// SetAiming(false)でAdsComponent.IsAimingがfalseになること。
    /// </summary>
    [Fact]
    public void SetAimingFalseSetsIsAimingFalse()
    {
        var world = new World();
        var router = new Router();
        var system = new WeaponSystem(world, router);
        var id = new EntityId("player#1");
        world.Register(id);
        world.SetComponent(id, new AdsComponent(IsAiming: true));

        system.SetAiming(id, false);

        world.GetComponent<AdsComponent>(id)!.IsAiming.ShouldBeFalse();
    }
}
