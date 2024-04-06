namespace Celerio;

public interface ILogger
{
    public void Log(string level, string message, params object[] args);
}

public static class Logging
{
    public static ILogger Logger = new Logger();

    public static void Log(string message, params object[] args) => Logger.Log("INFO", message, args);
    public static void Warn(string message, params object[] args) => Logger.Log("WARNING", message, args);
    public static void Err(string message, params object[] args) => Logger.Log("ERROR", message, args);
    
    public static void Message(string level, string message, params object[] args) => Logger.Log(level, message, args);

}

public class Logger : ILogger
{
    public void Log(string level, string message, params object[] args)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{level}] {message}");
    }
}