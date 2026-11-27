using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartEMobilityHub.Core.Adapters;

namespace SmartEMobilityHub.Operator.Infrastructure;

/// <summary>
/// Hiển thị nhật ký của phiên mạng lưới (<see cref="SystemLog"/> của Core) ở trang "Kịch bản demo &amp; nhật ký".
/// Giờ ghi log theo đồng hồ mô phỏng (DC-53); cửa sổ mở sau vẫn thấy các dòng đã ghi trước đó.
/// </summary>
public sealed class UiLogger
{
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    public UiLogger(SystemLog log)
    {
        Source = log;
        foreach (var line in log.Lines) Lines.Add(line);
        log.LineAdded += OnLineAdded;
    }

    public SystemLog Source { get; }

    public ObservableCollection<string> Lines { get; } = new();

    private void OnLineAdded(string line)
    {
        if (_dispatcher.CheckAccess()) Push(line);
        else _dispatcher.Invoke(() => Push(line));
    }

    private void Push(string line)
    {
        Lines.Insert(0, line);
        while (Lines.Count > SystemLog.MaxLines) Lines.RemoveAt(Lines.Count - 1);
    }
}
