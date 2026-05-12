using tps.contract;
using VitalRouter;

namespace tps.csharp;

public sealed class WeaponSystem(World world, Router router)
{
    public bool TryFire(EntityId id)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null || !weapon.CanFire) return false;

        world.SetComponent(id, weapon with
        {
            Ammo = weapon.Ammo - 1,
            FireCooldown = weapon.FireInterval,
        });
        _ = router.PublishAsync(new ShotFiredCommand { AmmoLeft = weapon.Ammo - 1 });
        return true;
    }

    public bool TryStartReload(EntityId id)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null || weapon.IsReloading || weapon.Ammo == weapon.MagazineSize) return false;

        world.SetComponent(id, weapon with { ReloadTimer = weapon.ReloadDuration, Ammo = 0 });
        return true;
    }

    public void Update(EntityId id, float delta)
    {
        var weapon = world.GetComponent<WeaponComponent>(id);
        if (weapon is null) return;

        var reloadTimer = weapon.ReloadTimer;
        var fireCooldown = Math.Max(0f, weapon.FireCooldown - delta);
        var ammo = weapon.Ammo;

        if (reloadTimer > 0f)
        {
            reloadTimer -= delta;
            if (reloadTimer <= 0f)
            {
                reloadTimer = 0f;
                ammo = weapon.MagazineSize;
            }
        }

        world.SetComponent(id, weapon with
        {
            ReloadTimer = reloadTimer,
            FireCooldown = fireCooldown,
            Ammo = ammo,
        });
    }
}
