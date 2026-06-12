using UnitGenerator;

namespace gamekit;

[UnitOf(typeof(string), UnitGenerateOptions.JsonConverter)]
public readonly partial struct EntityId { }
