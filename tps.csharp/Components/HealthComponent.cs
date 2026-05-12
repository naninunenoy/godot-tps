namespace tps.csharp;

public record HealthComponent(int Hp, int MaxHp) : IComponent
{
    public bool IsAlive => Hp > 0;
}
