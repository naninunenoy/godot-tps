using System.Text.Json;
using Shouldly;

namespace gamekit.test;

public class EntityIdTest
{
    /// <summary>EntityId("player#1") を JSON シリアライズすると素の文字列 "player#1" になること（wire format の互換確認）。</summary>
    [Fact]
    public void Serialize_ProducesPlainString()
    {
        var json = JsonSerializer.Serialize(new EntityId("player#1"));

        json.ShouldBe("\"player#1\"");
    }

    /// <summary>JSON 文字列 "target#3" をデシリアライズすると EntityId("target#3") に復元されること。</summary>
    [Fact]
    public void Deserialize_RestoresEntityId()
    {
        var id = JsonSerializer.Deserialize<EntityId>("\"target#3\"");

        id.ShouldBe(new EntityId("target#3"));
    }
}
