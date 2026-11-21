using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Tests.Support;

public sealed class CollectingLogger : ISystemLogger
{
    public List<(string Level, string Message)> Entries { get; } = new();

    public void Info(string message, string? context = null) => Entries.Add(("INFO", message));
    public void Warn(string message, string? context = null) => Entries.Add(("WARN", message));
    public void Error(string message, string? context = null) => Entries.Add(("ERROR", message));

    public bool Contains(string level, string fragment) =>
        Entries.Any(e => e.Level == level && e.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
