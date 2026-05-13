using System;
using R3;

namespace tps.csharp;

public class Health : IDisposable
{
    public int Max { get; }
    public bool IsAlive => _current.CurrentValue > 0;

    private readonly ReactiveProperty<int> _current;
    private readonly Subject<Unit> _onDied = new();

    public ReadOnlyReactiveProperty<int> Current => _current;
    public Observable<Unit> OnDied => _onDied;

    public Health(int max)
    {
        Max = max;
        _current = new ReactiveProperty<int>(max);
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
            return;
        _current.Value = Math.Max(0, _current.CurrentValue - amount);
        if (_current.CurrentValue == 0)
            _onDied.OnNext(Unit.Default);
    }

    public void Reset()
    {
        _current.Value = Max;
    }

    public void Dispose()
    {
        _current.Dispose();
        _onDied.Dispose();
    }
}
