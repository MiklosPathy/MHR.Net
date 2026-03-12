// Debug console helper - writes to log file and Debug output
using System.Diagnostics;

namespace MHR;

/// <summary>
/// Redirects Console.WriteLine calls to a log file and Debug output.
/// </summary>
public static class DebugConsole
{
    private static readonly string LogFilePath;
    private static readonly object LockObj = new();

    static DebugConsole()
    {
        // Create log file in the app directory
        LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mhr_debug.log");

        // Clear the log file on startup
        try
        {
            File.WriteAllText(LogFilePath, $"=== MHR Debug Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
        }
        catch
        {
            // Ignore if we can't write
        }
    }

    public static void WriteLine(string? message = null)
    {
        var line = message ?? "";
        Debug.WriteLine(line);
        AppendToLog(line);
    }

    public static void WriteLine(object? value)
    {
        var line = value?.ToString() ?? "";
        Debug.WriteLine(line);
        AppendToLog(line);
    }

    public static void WriteLine(string format, params object?[] args)
    {
        var line = string.Format(format, args);
        Debug.WriteLine(line);
        AppendToLog(line);
    }

    public static void Write(string? message)
    {
        var text = message ?? "";
        Debug.Write(text);
        AppendToLog(text, newLine: false);
    }

    public static void Write(object? value)
    {
        var text = value?.ToString() ?? "";
        Debug.Write(text);
        AppendToLog(text, newLine: false);
    }

    private static void AppendToLog(string text, bool newLine = true)
    {
        try
        {
            lock (LockObj)
            {
                File.AppendAllText(LogFilePath, newLine ? text + "\n" : text);
            }
        }
        catch
        {
            // Ignore write errors
        }
    }

    /// <summary>
    /// Gets the path to the log file.
    /// </summary>
    public static string GetLogFilePath() => LogFilePath;
}
