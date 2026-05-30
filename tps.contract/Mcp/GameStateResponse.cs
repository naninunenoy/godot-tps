namespace tps.contract.Mcp;

public record GameStateResponse(ulong FrameCount, int ObjectCount, ObjectSnapshotDto[] Objects);

public record ObjectSnapshotDto(string Id, string Name, HealthDto? Health, WeaponDto? Weapon, BoundsDto? Bounds);

public record HealthDto(int Hp, int MaxHp);

public record WeaponDto(int Ammo, int MagazineSize, bool IsReloading, Vec3Dto? MuzzlePosition, Vec3Dto? MuzzleDirection, bool? IsAiming = null);

public record BoundsDto(Vec3Dto Min, Vec3Dto Max);

public record Vec3Dto(float X, float Y, float Z);
