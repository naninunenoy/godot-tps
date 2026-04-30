using System;

namespace tps.csharp;

public class WeaponState
{
    public int MagazineSize { get; }
    public int CurrentAmmo { get; private set; }
    public float ReloadDuration { get; }
    public float FireInterval { get; }

    private float _reloadTimer;
    private float _fireCooldown;

    public bool IsReloading => _reloadTimer > 0f;
    public bool CanFire => !IsReloading && _fireCooldown <= 0f && CurrentAmmo > 0;
    public bool NeedsReload => CurrentAmmo == 0 && !IsReloading;

    public WeaponState(int magazineSize, float reloadDuration, float fireInterval)
    {
        MagazineSize = magazineSize;
        ReloadDuration = reloadDuration;
        FireInterval = fireInterval;
        CurrentAmmo = magazineSize;
    }

    public void Update(float delta)
    {
        if (_reloadTimer > 0f)
        {
            _reloadTimer -= delta;
            if (_reloadTimer <= 0f)
            {
                _reloadTimer = 0f;
                CurrentAmmo = MagazineSize;
            }
        }
        if (_fireCooldown > 0f)
            _fireCooldown = Math.Max(0f, _fireCooldown - delta);
    }

    public bool TryFire()
    {
        if (!CanFire) return false;
        CurrentAmmo--;
        _fireCooldown = FireInterval;
        return true;
    }

    public bool TryStartReload()
    {
        if (IsReloading || CurrentAmmo == MagazineSize) return false;
        _reloadTimer = ReloadDuration;
        CurrentAmmo = 0;
        return true;
    }
}
