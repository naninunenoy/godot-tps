using Shouldly;
using tps.contract;
using VitalRouter;

namespace tps.csharp.test;

public class KillSystemTest
{
    /// <summary>
    /// 生成直後にKillCountが0であること。
    /// </summary>
    [Fact]
    public void KillCountStartsAtZero()
    {
        var router = new Router();
        using var system = new KillSystem(router);
        system.KillCount.ShouldBe(0);
    }

    /// <summary>
    /// TargetDestroyedCommandを受信するとKillCountが1になること。
    /// </summary>
    [Fact]
    public async Task IncrementOnTargetDestroyed()
    {
        var router = new Router();
        using var system = new KillSystem(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#1" });
        system.KillCount.ShouldBe(1);
    }

    /// <summary>
    /// TargetDestroyedCommandを3回受信するとKillCountが3に累積されること。
    /// </summary>
    [Fact]
    public async Task MultipleKillsAccumulate()
    {
        var router = new Router();
        using var system = new KillSystem(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#1" });
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#2" });
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#3" });
        system.KillCount.ShouldBe(3);
    }

    /// <summary>
    /// TargetDestroyedCommand受信後にKillCountChangedCommandが正しいカウント(1)でpublishされること。
    /// </summary>
    [Fact]
    public async Task KillCountChangedCommandPublishedWithCorrectCount()
    {
        var router = new Router();
        using var system = new KillSystem(router);
        int receivedCount = -1;
        router.Subscribe<KillCountChangedCommand>((cmd, _) => receivedCount = cmd.Count);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#1" });
        receivedCount.ShouldBe(1);
    }

    /// <summary>
    /// Dispose後はTargetDestroyedCommandを受信してもKillCountが増えないこと（1のまま）。
    /// </summary>
    [Fact]
    public async Task DisposedSystemStopsReceivingCommands()
    {
        var router = new Router();
        var system = new KillSystem(router);
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#1" });
        system.Dispose();
        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#2" });
        system.KillCount.ShouldBe(1);
    }

    /// <summary>
    /// TargetDestroyedCommand受信後にKillCountChangedイベントがCount=1でログに記録され、エラーがないこと。
    /// </summary>
    [Fact]
    public async Task OnTargetDestroyed_LogsKillCountChanged()
    {
        var router = new Router();
        var logStore = new InMemoryLogStore();
        using var system = new KillSystem(router, logStore);

        await router.PublishAsync(new TargetDestroyedCommand { TargetName = "target#1" });

        logStore.HasEvent(GameEvents.KillCountChanged, p => (int?)p["Count"] == 1).ShouldBeTrue();
        logStore.Errors.ShouldBeEmpty();
    }
}
