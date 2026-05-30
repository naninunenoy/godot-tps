using Microsoft.Extensions.Logging;
using Shouldly;

namespace tps.csharp.test;

public class LogStoreTest
{
    /// <summary>
    /// Add() でエントリを1件追加したとき、Entries に1件保存されること。
    /// </summary>
    [Fact]
    public void Add_StoresEntry()
    {
        var store = new InMemoryLogStore();
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ShotFired,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Entries.Count.ShouldBe(1);
    }

    /// <summary>
    /// 複数エントリを追加後に Clear() を呼んだとき、Entries が空になること。
    /// </summary>
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var store = new InMemoryLogStore();
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ShotFired,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.TargetHit,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Clear();
        store.Entries.ShouldBeEmpty();
    }

    /// <summary>
    /// Information・Warning・Error・Critical の4件を追加したとき、
    /// Errors には Error 以上の2件のみ返ること。
    /// </summary>
    [Fact]
    public void Errors_OnlyReturnsErrorAndAbove()
    {
        var store = new InMemoryLogStore();
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ShotFired,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Add(
            new GameLogEntry(
                LogLevel.Warning,
                GameEvents.TargetHit,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Add(
            new GameLogEntry(
                LogLevel.Error,
                "SomeError",
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Add(
            new GameLogEntry(
                LogLevel.Critical,
                "SomeCritical",
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Errors.Count.ShouldBe(2);
    }

    /// <summary>
    /// HasEvent() にイベント種別を渡したとき、
    /// 追加済みのイベントは true、未追加のイベントは false を返すこと。
    /// </summary>
    [Fact]
    public void HasEvent_FindsByEventType()
    {
        var store = new InMemoryLogStore();
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ReloadStarted,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.HasEvent(GameEvents.ReloadStarted).ShouldBeTrue();
        store.HasEvent(GameEvents.ShotFired).ShouldBeFalse();
    }

    /// <summary>
    /// HasEvent() にプロパティ条件（述語）を渡したとき、
    /// プロパティが一致するエントリがある場合は true、一致しない場合は false を返すこと。
    /// </summary>
    [Fact]
    public void HasEvent_WithPredicate_FiltersOnProperties()
    {
        var store = new InMemoryLogStore();
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.TargetHit,
                new Dictionary<string, object?> { ["EntityId"] = "target#1", ["Damage"] = 30 },
                DateTimeOffset.UtcNow
            )
        );

        store.HasEvent(GameEvents.TargetHit, p => (int?)p["Damage"] == 30).ShouldBeTrue();
        store.HasEvent(GameEvents.TargetHit, p => (int?)p["Damage"] == 99).ShouldBeFalse();
    }

    /// <summary>
    /// Information・Debug のみのエントリを追加したとき、Errors が空になること。
    /// </summary>
    [Fact]
    public void Errors_IsEmptyWhenNoErrors()
    {
        var store = new InMemoryLogStore();
        store.Add(
            new GameLogEntry(
                LogLevel.Information,
                GameEvents.ShotFired,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Add(
            new GameLogEntry(
                LogLevel.Debug,
                GameEvents.ReloadStarted,
                new Dictionary<string, object?>(),
                DateTimeOffset.UtcNow
            )
        );
        store.Errors.ShouldBeEmpty();
    }
}
