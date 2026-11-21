using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>Logger mặc định cho demo; test dùng logger thu thập riêng.</summary>
public sealed class ConsoleSystemLogger : ISystemLogger
{
    public void Info(string message, string? context = null) => Write("INFO", message, context);
    public void Warn(string message, string? context = null) => Write("WARN", message, context);
    public void Error(string message, string? context = null) => Write("ERROR", message, context);

    private static void Write(string level, string message, string? context)
    {
        var suffix = string.IsNullOrWhiteSpace(context) ? "" : $" [{context}]";
        Console.WriteLine($"{level,-5} {message}{suffix}");
    }
}
