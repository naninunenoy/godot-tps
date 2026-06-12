using System.Numerics;
using Shouldly;
using tps.contract.Mcp;

namespace tps.csharp.test;

public class GameStateResponseBuilderTest
{
    private sealed class StubSceneQuery(ulong frameCount, IReadOnlyList<IObjectSnapshot> snapshot)
        : ISceneQuery
    {
        public ulong FrameCount => frameCount;
        public int ObjectCount => snapshot.Count;
        public IReadOnlyList<IObjectSnapshot> Snapshot => snapshot;
    }

    private static Entity CreateEntity(string name)
    {
        var world = new World();
        var id = new EntityId(name);
        world.Register(id);
        return new Entity(id, name, world);
    }

    /// <summary>HealthComponent(2, 3) を持つ Entity が HealthDto(Hp=2, MaxHp=3) にマップされ、Weapon / Bounds は null になること。FrameCount=10・ObjectCount=1 もそのまま反映されること。</summary>
    [Fact]
    public void Build_MapsHealthComponent()
    {
        var entity = CreateEntity("target#1");
        entity.Set(new HealthComponent(2, 3));
        var query = new StubSceneQuery(10, [entity.Snapshot()]);

        var response = GameStateResponseBuilder.Build(query);

        response.FrameCount.ShouldBe(10UL);
        response.ObjectCount.ShouldBe(1);
        var obj = response.Objects.ShouldHaveSingleItem();
        obj.Id.ShouldBe("target#1");
        obj.Name.ShouldBe("target#1");
        obj.Health.ShouldBe(new HealthDto(2, 3));
        obj.Weapon.ShouldBeNull();
        obj.Bounds.ShouldBeNull();
    }

    /// <summary>Weapon / Transform / Camera / Ads を持つ Entity の WeaponDto に、残弾 7/30・銃口位置 (Transform.Position の Y+1.3)・銃口方向 (カメラ前方 (0,0,-1))・IsAiming=true が入ること。</summary>
    [Fact]
    public void Build_MapsWeaponWithMuzzleInfo()
    {
        var entity = CreateEntity("player");
        entity.Set(new WeaponComponent(7, 30, 0f, 0f, 2f, 0.1f));
        entity.Set(new TransformComponent(new Vector3(1f, 2f, 3f), Vector3.Zero));
        entity.Set(new CameraComponent(new Vector3(0f, 0f, -1f)));
        entity.Set(new AdsComponent(IsAiming: true));
        var query = new StubSceneQuery(1, [entity.Snapshot()]);

        var obj = GameStateResponseBuilder.Build(query).Objects.ShouldHaveSingleItem();

        obj.Weapon.ShouldNotBeNull();
        obj.Weapon.Ammo.ShouldBe(7);
        obj.Weapon.MagazineSize.ShouldBe(30);
        obj.Weapon.IsReloading.ShouldBeFalse();
        obj.Weapon.MuzzlePosition.ShouldNotBeNull();
        obj.Weapon.MuzzlePosition.X.ShouldBe(1f);
        obj.Weapon.MuzzlePosition.Y.ShouldBe(3.3f, 0.0001);
        obj.Weapon.MuzzlePosition.Z.ShouldBe(3f);
        obj.Weapon.MuzzleDirection.ShouldBe(new Vec3Dto(0f, 0f, -1f));
        obj.Weapon.IsAiming.ShouldBe(true);
    }

    /// <summary>Transform / Camera を持たない Entity の WeaponDto は、MuzzlePosition / MuzzleDirection / IsAiming が null になること。</summary>
    [Fact]
    public void Build_WeaponWithoutTransformAndCamera_HasNullMuzzleInfo()
    {
        var entity = CreateEntity("player");
        entity.Set(new WeaponComponent(30, 30, 0f, 0f, 2f, 0.1f));
        var query = new StubSceneQuery(1, [entity.Snapshot()]);

        var obj = GameStateResponseBuilder.Build(query).Objects.ShouldHaveSingleItem();

        obj.Weapon.ShouldNotBeNull();
        obj.Weapon.MuzzlePosition.ShouldBeNull();
        obj.Weapon.MuzzleDirection.ShouldBeNull();
        obj.Weapon.IsAiming.ShouldBeNull();
    }

    /// <summary>BoundsComponent(Min=(-1,0,-2), Max=(1,2,3)) が BoundsDto の Min / Max にそのままマップされること。</summary>
    [Fact]
    public void Build_MapsBoundsComponent()
    {
        var entity = CreateEntity("target#1");
        entity.Set(new BoundsComponent(new Vector3(-1f, 0f, -2f), new Vector3(1f, 2f, 3f)));
        var query = new StubSceneQuery(1, [entity.Snapshot()]);

        var obj = GameStateResponseBuilder.Build(query).Objects.ShouldHaveSingleItem();

        obj.Bounds.ShouldBe(new BoundsDto(new Vec3Dto(-1f, 0f, -2f), new Vec3Dto(1f, 2f, 3f)));
    }

    /// <summary>コンポーネントを 1 つも持たない Entity は、Id / Name のみ設定され Health / Weapon / Bounds がすべて null になること。</summary>
    [Fact]
    public void Build_EmptyEntity_HasNullComponents()
    {
        var entity = CreateEntity("ghost");
        var query = new StubSceneQuery(1, [entity.Snapshot()]);

        var obj = GameStateResponseBuilder.Build(query).Objects.ShouldHaveSingleItem();

        obj.Id.ShouldBe("ghost");
        obj.Health.ShouldBeNull();
        obj.Weapon.ShouldBeNull();
        obj.Bounds.ShouldBeNull();
    }
}
