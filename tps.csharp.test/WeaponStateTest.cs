using R3;
using Shouldly;

namespace tps.csharp.test;

public class WeaponStateTest
{
    /// <summary>
    /// 生成直後にCurrentAmmoがマガジン最大(10)であり、CanFireがtrueであること。
    /// </summary>
    [Fact]
    public void StartsWithFullMagazine()
    {
        var w = new WeaponState(10, 2f, 0.1f);
        w.CurrentAmmo.CurrentValue.ShouldBe(10);
        w.CanFire.ShouldBeTrue();
    }

    /// <summary>
    /// TryFire()がtrueを返し、CurrentAmmoが1減ること（10→9）。
    /// </summary>
    [Fact]
    public void TryFireDecreasesAmmo()
    {
        var w = new WeaponState(10, 2f, 0.1f);
        w.TryFire().ShouldBeTrue();
        w.CurrentAmmo.CurrentValue.ShouldBe(9);
    }

    /// <summary>
    /// TryFire()直後はクールダウン中のためCanFireがfalseになること。
    /// </summary>
    [Fact]
    public void CannotFireDuringCooldown()
    {
        var w = new WeaponState(10, 2f, 0.5f);
        w.TryFire();
        w.CanFire.ShouldBeFalse();
    }

    /// <summary>
    /// クールダウン以上のdeltaでUpdate後にCanFireがtrueに戻ること。
    /// </summary>
    [Fact]
    public void CooldownExpiresAfterUpdate()
    {
        var w = new WeaponState(10, 2f, 0.1f);
        w.TryFire();
        w.Update(0.2f);
        w.CanFire.ShouldBeTrue();
    }

    /// <summary>
    /// 弾薬が0になるとCanFireがfalse・NeedsReloadがtrueになること。
    /// </summary>
    [Fact]
    public void CannotFireWhenEmpty()
    {
        var w = new WeaponState(1, 2f, 0.1f);
        w.TryFire();
        w.Update(1f);
        w.CanFire.ShouldBeFalse();
        w.NeedsReload.ShouldBeTrue();
    }

    /// <summary>
    /// リロード時間を超えてUpdate後に弾薬がマガジン最大(5)に戻り、IsReloadingがfalseになること。
    /// </summary>
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

    /// <summary>
    /// 弾薬が満タンのときTryStartReload()がfalseを返すこと。
    /// </summary>
    [Fact]
    public void CannotReloadWhenFull()
    {
        var w = new WeaponState(5, 1f, 0.1f);
        w.TryStartReload().ShouldBeFalse();
    }

    /// <summary>
    /// TryFire()を2回呼ぶとOnFiredが2回発火すること。
    /// </summary>
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

    /// <summary>
    /// リロード時間を超えてUpdate後にOnReloadCompletedが発火すること。
    /// </summary>
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
