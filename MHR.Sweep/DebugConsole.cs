using System.Diagnostics;

namespace MHR.Sweep;

public static class DebugConsole
{
    private static readonly string LogFilePath;
    private static readonly object LockObj = new();

    static DebugConsole()
    {
        LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sweep_debug.log");
        try
        {
            File.WriteAllText(LogFilePath, $"=== MHR Sweep Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
        }
        catch { }
    }

    public static void WriteLine(string? message = null)
    {
        var line = message ?? "";
        Debug.WriteLine(line);
        System.Console.WriteLine(line);
        AppendToLog(line);
    }

    public static void WriteLine(object? value) => WriteLine(value?.ToString());

    private static void AppendToLog(string text)
    {
        try { lock (LockObj) { File.AppendAllText(LogFilePath, text + "\n"); } }
        catch { }
    }
}
