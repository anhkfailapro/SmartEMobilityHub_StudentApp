using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Operator.Infrastructure;

/// <summary>Ghi log vào bộ nhớ để hiển thị ở cột phải dashboard.</summary>
public sealed class UiLogger : ISystemLogger
{
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    public ObservableCollection<string> Lines { get; } = new();

    public void Info(string message, string? context = null) => Add("INFO", message);
    public void Warn(string message, string? context = null) => Add("WARN", message);
    public void Error(string message, string? context = null) => Add("ERROR", message);

    private void Add(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss}  {level,-5}  {message}";

        if (_dispatcher.CheckAccess()) Push(line);
        else _dispatcher.Invoke(() => Push(line));
    }

    private void Push(string line)
    {
        Lines.Insert(0, line);
        while (Lines.Count > 50) Lines.RemoveAt(Lines.Count - 1);
    }
}
