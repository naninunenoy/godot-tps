using tps.contract;
using VitalRouter;

namespace tps.csharp;

[Routes]
public sealed partial class KillSystem : IDisposable
{
    private readonly Router _router;
    private int _killCount;
    private IDisposable? _subscription;

    public int KillCount => _killCount;

    public KillSystem(Router router)
    {
        _router = router;
        _subscription = this.MapTo(router);
    }

    [Route]
    public async ValueTask On(TargetDestroyedCommand _)
    {
        _killCount++;
        await _router.PublishAsync(new KillCountChangedCommand { Count = _killCount });
    }

    public void Dispose() => _subscription?.Dispose();
}
