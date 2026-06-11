using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.UseFilter<ErrorFilter>();
app.Add<GameCommands>();
app.Add<TpsCommands>();
app.Run(args);

class ErrorFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
