namespace gamekit;

public interface IScene
{
    IReadOnlyList<ICommandDescriptor> AvailableCommands { get; }
}

public interface ICommandDescriptor
{
    Type CommandType { get; }
    string Name { get; }
}

public static class CommandDescriptor
{
    public static ICommandDescriptor Of<T>() => new Impl(typeof(T));

    private sealed record Impl(Type CommandType) : ICommandDescriptor
    {
        public string Name => CommandType.Name;
    }
}
