using UnitGenerator;

namespace tps.csharp;

[UnitOf(typeof(string), UnitGenerateOptions.JsonConverter)]
public readonly partial struct EntityId { }
