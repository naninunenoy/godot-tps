using R3;

namespace tps.csharp;

public sealed class KillCounter : IDisposable
{
    private readonly ReactiveProperty<int> _count = new(0);
    public ReadOnlyReactiveProperty<int> Count => _count;

    public void Increment() => _count.Value++;

    public void Dispose() => _count.Dispose();
}
