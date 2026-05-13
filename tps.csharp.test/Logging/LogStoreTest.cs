using Microsoft.Extensions.Logging;
using Shouldly;

namespace tps.csharp.test;

public class LogStoreTest
{
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
