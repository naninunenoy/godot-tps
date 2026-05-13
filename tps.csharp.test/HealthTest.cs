using R3;
using Shouldly;

namespace tps.csharp.test;

public class HealthTest
{
    /// <summary>
    /// 生成直後にCurrentがmaxHp(100)であり、IsAliveがtrueであること。
    /// </summary>
    [Fact]
    public void StartsAtMax()
    {
        var hp = new Health(100);
        hp.Current.CurrentValue.ShouldBe(100);
        hp.IsAlive.ShouldBeTrue();
    }

    /// <summary>
    /// TakeDamage(30)でCurrentが100から70に減ること。
    /// </summary>
    [Fact]
    public void TakeDamageReducesCurrent()
    {
        var hp = new Health(100);
        hp.TakeDamage(30);
        hp.Current.CurrentValue.ShouldBe(70);
    }

    /// <summary>
    /// HPが0になるとIsAliveがfalseになり、OnDiedが発火すること。
    /// </summary>
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

    /// <summary>
    /// 死亡後にTakeDamageを呼んでもCurrentが0のままであること。
    /// </summary>
    [Fact]
    public void NoDamageAfterDeath()
    {
        var hp = new Health(10);
        hp.TakeDamage(10);
        hp.TakeDamage(99);
        hp.Current.CurrentValue.ShouldBe(0);
    }

    /// <summary>
    /// Reset()でCurrentがmaxHp(50)に戻り、IsAliveがtrueに戻ること。
    /// </summary>
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
