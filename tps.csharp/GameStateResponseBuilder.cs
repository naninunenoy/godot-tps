using tps.contract.Mcp;

namespace tps.csharp;

/// <summary>
/// ISceneQuery のスナップショットを外部公開用の GameStateResponse に変換する。
/// Godot 非依存の純粋なマッピングなので xUnit でテスト可能。
/// </summary>
public static class GameStateResponseBuilder
{
    public static GameStateResponse Build(ISceneQuery sceneQuery)
    {
        var objects = sceneQuery.Snapshot.Select(obj =>
        {
            var health = obj.GetComponent<HealthComponent>();
            var weapon = obj.GetComponent<WeaponComponent>();
            var transform = obj.GetComponent<TransformComponent>();
            var camera = obj.GetComponent<CameraComponent>();
            var bounds = obj.GetComponent<BoundsComponent>();

            WeaponDto? weaponDto = null;
            if (weapon is not null)
            {
                Vec3Dto? muzzlePos = null;
                Vec3Dto? muzzleDir = null;
                if (transform is not null)
                {
                    var pos = transform.Position;
                    muzzlePos = new Vec3Dto(pos.X, pos.Y + 1.3f, pos.Z);
                }
                if (camera is not null)
                    muzzleDir = new Vec3Dto(camera.Forward.X, camera.Forward.Y, camera.Forward.Z);
                var ads = obj.GetComponent<AdsComponent>();
                weaponDto = new WeaponDto(weapon.CurrentAmmo, weapon.MagazineSize, weapon.IsReloading, muzzlePos, muzzleDir, ads?.IsAiming);
            }

            BoundsDto? boundsDto = null;
            if (bounds is not null)
                boundsDto = new BoundsDto(
                    new Vec3Dto(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
                    new Vec3Dto(bounds.Max.X, bounds.Max.Y, bounds.Max.Z)
                );

            return new ObjectSnapshotDto(
                obj.Id.AsPrimitive(),
                obj.Name,
                health is not null ? new HealthDto(health.Hp, health.MaxHp) : null,
                weaponDto,
                boundsDto
            );
        }).ToArray();
        return new GameStateResponse(sceneQuery.FrameCount, sceneQuery.ObjectCount, objects);
    }
}
