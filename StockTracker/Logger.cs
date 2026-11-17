public static class Logger
{
    public static void Info(string message) => Write("INFO", message);

    public static void Alert(string message) => Write("ALERT", message);

    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        if (level == "ERROR")
            Console.Error.WriteLine(log);
        else
            Console.WriteLine(log);
    }
}
