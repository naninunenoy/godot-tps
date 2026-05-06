using Shouldly;
using tps.contract;
using tps.csharp;
using VitalRouter;

namespace tps.csharp.test;

public class KillCounterTest
{
    [Fact]
    public void CountStartsAtZero()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        counter.Count.CurrentValue.ShouldBe(0);
    }

    [Fact]
    public async Task IncrementOnTargetDestroyed()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target1" });
        counter.Count.CurrentValue.ShouldBe(1);
    }

    [Fact]
    public async Task MultipleKillsAccumulate()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target1" });
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target2" });
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target3" });
        counter.Count.CurrentValue.ShouldBe(3);
    }

    [Fact]
    public async Task KillCountChangedCommandPublishedWithCorrectCount()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        int receivedCount = -1;
        router.Subscribe<KillCountChangedCommand>((cmd, _) => { receivedCount = cmd.Count; });
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target1" });
        receivedCount.ShouldBe(1);
    }

    [Fact]
    public async Task DisposedCounterStopsReceivingCommands()
    {
        var router = new Router();
        var counter = new KillCounter(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target1" });
        counter.Dispose();
        // Dispose 後は購読解除済みなので例外なく完了する
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target2" });
    }
}
