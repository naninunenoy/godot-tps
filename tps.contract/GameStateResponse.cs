namespace tps.contract;

public record GameStateResponse(
    ulong FrameCount,
    int ObjectCount,
    ObjectSnapshotDto[] Objects);

public record ObjectSnapshotDto(
    string Id,
    string Name,
    HealthDto? Health,
    WeaponDto? Weapon);

public record HealthDto(int Hp, int MaxHp);
public record WeaponDto(int Ammo, int MagazineSize, bool IsReloading);
