namespace tps.csharp;

public record WeaponComponent(
    int CurrentAmmo,
    int MagazineSize,
    float ReloadTimer,
    float FireCooldown,
    float ReloadDuration,
    float FireInterval,
    float BulletSpeed = 80f,
    int BulletDamage = 1
) : IComponent
{
    public bool IsReloading => ReloadTimer > 0f;
    public bool CanFire => !IsReloading && FireCooldown <= 0f && CurrentAmmo > 0;
    public bool NeedsReload => CurrentAmmo == 0 && !IsReloading;
}
