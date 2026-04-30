using System;

namespace tps.csharp;

public class Health
{
    public int Max { get; }
    public int Current { get; private set; }
    public bool IsAlive => Current > 0;

    public event Action OnDied;

    public Health(int max)
    {
        Max = max;
        Current = max;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        Current = Math.Max(0, Current - amount);
        if (Current == 0)
            OnDied?.Invoke();
    }

    public void Reset()
    {
        Current = Max;
    }
}
