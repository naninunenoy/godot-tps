using Microsoft.Extensions.Logging;
using tps.contract.GameCommand;
using VitalRouter;

namespace tps.csharp;

public sealed class WeaponSystem(World world, Router router, ILogStore? logStore = null)
{
    public void SetAiming(EntityId id, bool isAiming)
    {
        var ads = world.GetComponent<AdsComponent>(id);
        if (ads is null) return;
        world.SetComponent(id, ads with { IsAiming = isAiming });
    }

    public bool TryFire(EntityId id)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null || !weapon.CanFire)
            return false;

        var ads = world.GetComponent<AdsComponent>(id);
        if (ads is null || !ads.IsAiming)
            return false;

        var camera = world.GetComponent<CameraComponent>(id);
        var direction = camera?.Forward ?? System.Numerics.Vector3.UnitZ;

        var ammoLeft = weapon.CurrentAmmo - 1;
        world.SetComponent(id, weapon with { CurrentAmmo = ammoLeft, FireCooldown = weapon.FireInterval });

        _ = router.PublishAsync(new ShotFiredCommand { AmmoLeft = ammoLeft });
        _ = router.PublishAsync(new BulletSpawnRequested
        {
            Direction = direction,
            Speed = weapon.BulletSpeed,
            Damage = weapon.BulletDamage,
        });

        logStore?.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ShotFired,
                new Dictionary<string, object?>
                {
                    ["EntityId"] = id.AsPrimitive(),
                    ["AmmoLeft"] = ammoLeft,
                },
                DateTimeOffset.UtcNow
            )
        );

        return true;
    }

    public bool TryStartReload(EntityId id)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null || weapon.IsReloading || weapon.CurrentAmmo == weapon.MagazineSize)
            return false;

        world.SetComponent(id, weapon with { ReloadTimer = weapon.ReloadDuration, CurrentAmmo = 0 });

        logStore?.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ReloadStarted,
                new Dictionary<string, object?> { ["EntityId"] = id.AsPrimitive() },
                DateTimeOffset.UtcNow
            )
        );

        return true;
    }

    public void Update(EntityId id, float delta)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null)
            return;

        var reloadTimer = weapon.ReloadTimer;
        var fireCooldown = Math.Max(0f, weapon.FireCooldown - delta);
        var ammo = weapon.CurrentAmmo;
        var reloadCompleted = false;

        if (reloadTimer > 0f)
        {
            reloadTimer -= delta;
            if (reloadTimer <= 0f)
            {
                reloadTimer = 0f;
                ammo = weapon.MagazineSize;
                reloadCompleted = true;
            }
        }

        world.SetComponent(
            id,
            weapon with
            {
                ReloadTimer = reloadTimer,
                FireCooldown = fireCooldown,
                CurrentAmmo = ammo,
            }
        );

        if (reloadCompleted)
            logStore?.Add(
                new GameLogEntry(
                    LogLevel.Information,
                    GameEvents.ReloadCompleted,
                    new Dictionary<string, object?>
                    {
                        ["EntityId"] = id.AsPrimitive(),
                        ["Ammo"] = ammo,
                    },
                    DateTimeOffset.UtcNow
                )
            );
    }
}
