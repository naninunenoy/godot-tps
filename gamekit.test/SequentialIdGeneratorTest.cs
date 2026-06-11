using Shouldly;

namespace gamekit.test;

public class SequentialIdGeneratorTest
{
    /// <summary>Next("player") を2回呼んだとき、"player#1"・"player#2" と連番の EntityId が生成されること。</summary>
    [Fact]
    public void Next_GeneratesSequentialIds()
    {
        var generator = new SequentialIdGenerator();

        generator.Next("player").AsPrimitive().ShouldBe("player#1");
        generator.Next("player").AsPrimitive().ShouldBe("player#2");
    }

    /// <summary>prefix が異なってもカウンタは共通で、Next("a")→"a#1"、Next("b")→"b#2" となること。</summary>
    [Fact]
    public void Next_SharesCounterAcrossPrefixes()
    {
        var generator = new SequentialIdGenerator();

        generator.Next("a").AsPrimitive().ShouldBe("a#1");
        generator.Next("b").AsPrimitive().ShouldBe("b#2");
    }
}
