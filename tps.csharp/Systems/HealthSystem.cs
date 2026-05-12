using tps.contract;
using VitalRouter;

namespace tps.csharp;

public sealed class HealthSystem(World world, Router router)
{
    public bool TakeDamage(EntityId id, int amount)
    {
        var hp = world.GetComponent<HealthComponent>(id);
        if (hp is null || !hp.IsAlive) return false;

        var newHp = Math.Max(0, hp.Hp - amount);
        world.SetComponent(id, hp with { Hp = newHp });

        if (newHp == 0)
            _ = router.PublishAsync(new TargetDestroyedCommand { TargetName = id.AsPrimitive() });

        return true;
    }

    public void Reset(EntityId id)
    {
        var hp = world.GetComponent<HealthComponent>(id);
        if (hp is null) return;
        world.SetComponent(id, hp with { Hp = hp.MaxHp });
    }
}
