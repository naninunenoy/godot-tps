namespace gamekit;

public interface IIdGenerator
{
    EntityId Next(string prefix);
}

public sealed class SequentialIdGenerator : IIdGenerator
{
    private int _counter;

    public EntityId Next(string prefix) => new($"{prefix}#{++_counter}");
}
