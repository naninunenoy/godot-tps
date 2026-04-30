using R3;
using tps.csharp;
using Shouldly;

namespace tps.csharp.test;

public class WeaponStateTest
{
    [Fact]
    public void StartsWithFullMagazine()
    {
        var w = new WeaponState(10, 2f, 0.1f);
        w.CurrentAmmo.CurrentValue.ShouldBe(10);
        w.CanFire.ShouldBeTrue();
    }

    [Fact]
    public void TryFireDecreasesAmmo()
    {
        var w = new WeaponState(10, 2f, 0.1f);
        w.TryFire().ShouldBeTrue();
        w.CurrentAmmo.CurrentValue.ShouldBe(9);
    }

    [Fact]
    public void CannotFireDuringCooldown()
    {
        var w = new WeaponState(10, 2f, 0.5f);
        w.TryFire();
        w.CanFire.ShouldBeFalse();
    }

    [Fact]
    public void CooldownExpiresAfterUpdate()
    {
        var w = new WeaponState(10, 2f, 0.1f);
        w.TryFire();
        w.Update(0.2f);
        w.CanFire.ShouldBeTrue();
    }

    [Fact]
    public void CannotFireWhenEmpty()
    {
        var w = new WeaponState(1, 2f, 0.1f);
        w.TryFire();
        w.Update(1f);
        w.CanFire.ShouldBeFalse();
        w.NeedsReload.ShouldBeTrue();
    }

    [Fact]
    public void ReloadRestoresAmmoAfterDuration()
    {
        var w = new WeaponState(5, 1f, 0.1f);
        w.TryFire();
        w.TryStartReload();
        w.IsReloading.ShouldBeTrue();
        w.Update(1.1f);
        w.IsReloading.ShouldBeFalse();
        w.CurrentAmmo.CurrentValue.ShouldBe(5);
    }

    [Fact]
    public void CannotReloadWhenFull()
    {
        var w = new WeaponState(5, 1f, 0.1f);
        w.TryStartReload().ShouldBeFalse();
    }

    [Fact]
    public void OnFiredEmitsOnEachShot()
    {
        var w = new WeaponState(5, 2f, 0.1f);
        int count = 0;
        w.OnFired.Subscribe(_ => count++);
        w.TryFire();
        w.Update(0.2f);
        w.TryFire();
        count.ShouldBe(2);
    }

    [Fact]
    public void OnReloadCompletedEmitsAfterDuration()
    {
        var w = new WeaponState(5, 1f, 0.1f);
        bool reloaded = false;
        w.OnReloadCompleted.Subscribe(_ => reloaded = true);
        w.TryFire();
        w.TryStartReload();
        w.Update(1.1f);
        reloaded.ShouldBeTrue();
    }
}
