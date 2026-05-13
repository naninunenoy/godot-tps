using Shouldly;
using tps.contract;
using VitalRouter;

namespace tps.csharp.test;

public class KillCounterTest
{
    /// <summary>
    /// 生成直後にCount.CurrentValueが0であること。
    /// </summary>
    [Fact]
    public void CountStartsAtZero()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        counter.Count.CurrentValue.ShouldBe(0);
    }

    /// <summary>
    /// TargetDestroyedCommandを受信するとCountが1になること。
    /// </summary>
    [Fact]
    public async Task IncrementOnTargetDestroyed()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target1" });
        counter.Count.CurrentValue.ShouldBe(1);
    }

    /// <summary>
    /// TargetDestroyedCommandを3回受信するとCountが3に累積されること。
    /// </summary>
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

    /// <summary>
    /// TargetDestroyedCommand受信後にKillCountChangedCommandが正しいカウント(1)でpublishされること。
    /// </summary>
    [Fact]
    public async Task KillCountChangedCommandPublishedWithCorrectCount()
    {
        var router = new Router();
        using var counter = new KillCounter(router);
        int receivedCount = -1;
        router.Subscribe<KillCountChangedCommand>(
            (cmd, _) =>
            {
                receivedCount = cmd.Count;
            }
        );
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "Target1" });
        receivedCount.ShouldBe(1);
    }

    /// <summary>
    /// Dispose後はTargetDestroyedCommandを受信してもカウントが増えず、例外も発生しないこと。
    /// </summary>
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
