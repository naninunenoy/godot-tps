using Microsoft.Extensions.Logging;
using tps.contract;
using VitalRouter;

namespace tps.csharp;

public sealed class WeaponSystem(World world, Router router, ILogStore? logStore = null)
{
    public bool TryFire(EntityId id)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null || !weapon.CanFire)
            return false;

        var ammoLeft = weapon.Ammo - 1;
        world.SetComponent(id, weapon with { Ammo = ammoLeft, FireCooldown = weapon.FireInterval });
        _ = router.PublishAsync(new ShotFiredCommand { AmmoLeft = ammoLeft });

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
        if (weapon is null || weapon.IsReloading || weapon.Ammo == weapon.MagazineSize)
            return false;

        world.SetComponent(id, weapon with { ReloadTimer = weapon.ReloadDuration, Ammo = 0 });

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
        var ammo = weapon.Ammo;
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
                Ammo = ammo,
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
