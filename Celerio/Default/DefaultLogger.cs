namespace Celerio.DefaultPipeline;

public class DefaultLogger : ILogger
{
    public void Log(string level, string message, params object[] args)
    {
        //TODO: File/DB logging
        Console.WriteLine($"[{level}] {message}");
    }
}