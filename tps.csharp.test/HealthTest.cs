using R3;
using tps.csharp;
using Shouldly;

namespace tps.csharp.test;

public class HealthTest
{
    [Fact]
    public void StartsAtMax()
    {
        var hp = new Health(100);
        hp.Current.CurrentValue.ShouldBe(100);
        hp.IsAlive.ShouldBeTrue();
    }

    [Fact]
    public void TakeDamageReducesCurrent()
    {
        var hp = new Health(100);
        hp.TakeDamage(30);
        hp.Current.CurrentValue.ShouldBe(70);
    }

    [Fact]
    public void DiesWhenCurrentReachesZero()
    {
        bool died = false;
        var hp = new Health(10);
        hp.OnDied.Subscribe(_ => died = true);
        hp.TakeDamage(10);
        hp.IsAlive.ShouldBeFalse();
        died.ShouldBeTrue();
    }

    [Fact]
    public void NoDamageAfterDeath()
    {
        var hp = new Health(10);
        hp.TakeDamage(10);
        hp.TakeDamage(99);
        hp.Current.CurrentValue.ShouldBe(0);
    }

    [Fact]
    public void ResetRestoresMax()
    {
        var hp = new Health(50);
        hp.TakeDamage(40);
        hp.Reset();
        hp.Current.CurrentValue.ShouldBe(50);
        hp.IsAlive.ShouldBeTrue();
    }
}
