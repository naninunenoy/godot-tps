using System;
using R3;

namespace tps.csharp;

public class WeaponState : IDisposable
{
    public int MagazineSize { get; }
    public float ReloadDuration { get; }
    public float FireInterval { get; }

    private readonly ReactiveProperty<int> _currentAmmo;
    private readonly Subject<Unit> _onFired = new();
    private readonly Subject<Unit> _onReloadCompleted = new();

    private float _reloadTimer;
    private float _fireCooldown;

    public ReadOnlyReactiveProperty<int> CurrentAmmo => _currentAmmo;
    public Observable<Unit> OnFired => _onFired;
    public Observable<Unit> OnReloadCompleted => _onReloadCompleted;

    public bool IsReloading => _reloadTimer > 0f;
    public bool CanFire => !IsReloading && _fireCooldown <= 0f && _currentAmmo.Value > 0;
    public bool NeedsReload => _currentAmmo.Value == 0 && !IsReloading;

    public WeaponState(int magazineSize, float reloadDuration, float fireInterval)
    {
        MagazineSize = magazineSize;
        ReloadDuration = reloadDuration;
        FireInterval = fireInterval;
        _currentAmmo = new ReactiveProperty<int>(magazineSize);
    }

    public void Update(float delta)
    {
        if (_reloadTimer > 0f)
        {
            _reloadTimer -= delta;
            if (_reloadTimer <= 0f)
            {
                _reloadTimer = 0f;
                _currentAmmo.Value = MagazineSize;
                _onReloadCompleted.OnNext(Unit.Default);
            }
        }
        if (_fireCooldown > 0f)
            _fireCooldown = Math.Max(0f, _fireCooldown - delta);
    }

    public bool TryFire()
    {
        if (!CanFire) return false;
        _currentAmmo.Value--;
        _fireCooldown = FireInterval;
        _onFired.OnNext(Unit.Default);
        return true;
    }

    public bool TryStartReload()
    {
        if (IsReloading || _currentAmmo.Value == MagazineSize) return false;
        _reloadTimer = ReloadDuration;
        _currentAmmo.Value = 0;
        return true;
    }

    public void Dispose()
    {
        _currentAmmo.Dispose();
        _onFired.Dispose();
        _onReloadCompleted.Dispose();
    }
}
