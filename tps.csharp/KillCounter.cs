using R3;
using tps.contract;
using VitalRouter;

namespace tps.csharp;

[Routes]
public sealed partial class KillCounter : IDisposable
{
    private readonly Router _router;
    private readonly ReactiveProperty<int> _count = new(0);
    private IDisposable? _subscription;

    public ReadOnlyReactiveProperty<int> Count => _count;

    public KillCounter(Router router)
    {
        _router = router;
        _subscription = this.MapTo(router);
    }

    [Route]
    public async ValueTask On(TargetDestroyedCommand cmd)
    {
        _count.Value++;
        await _router.PublishAsync(new KillCountChangedCommand { Count = _count.Value });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _count.Dispose();
    }
}
