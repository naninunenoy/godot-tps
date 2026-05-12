namespace tps.csharp;

public record WeaponComponent(
    int Ammo,
    int MagazineSize,
    float ReloadTimer,
    float FireCooldown,
    float ReloadDuration,
    float FireInterval) : IComponent
{
    public bool IsReloading => ReloadTimer > 0f;
    public bool CanFire => !IsReloading && FireCooldown <= 0f && Ammo > 0;
    public bool NeedsReload => Ammo == 0 && !IsReloading;
}
