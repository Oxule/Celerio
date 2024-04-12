namespace Celerio;


public static class Logging
{
    public static void Log(string message, params object[] args) => Message("INFO", message, args);
    public static void Warn(string message, params object[] args) => Message("WARNING", message, args);
    public static void Err(string message, params object[] args) => Message("ERROR", message, args);
    
    public static void Message(string level, string message, params object[] args)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{level}] {message}");
    }
}