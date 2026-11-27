using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Ports;

namespace SmartEMobilityHub.Core.Adapters;

/// <summary>
/// Nhật ký hệ thống của một phiên mạng lưới (DC-54): giữ 50 dòng mới nhất, giờ theo đồng hồ mô phỏng.
/// Không phụ thuộc WPF; dashboard hiển thị qua <c>UiLogger</c> (đăng ký <see cref="LineAdded"/>).
/// </summary>
public sealed class SystemLog : ISystemLogger
{
    public const int MaxLines = 50;

    private readonly IClock _clock;
    private readonly List<string> _lines = new();

    public SystemLog(IClock clock) => _clock = clock;

    /// <summary>Mới nhất ở đầu danh sách.</summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>Phát mỗi khi có dòng mới (đã định dạng).</summary>
    public event Action<string>? LineAdded;

    public void Info(string message, string? context = null) => Add("INFO", message, context);
    public void Warn(string message, string? context = null) => Add("WARN", message, context);
    public void Error(string message, string? context = null) => Add("ERROR", message, context);

    public static string Format(DateTime at, string level, string message, string? context) =>
        string.IsNullOrWhiteSpace(context)
            ? $"{at:HH:mm:ss}  {level,-5}  {message}"
            : $"{at:HH:mm:ss}  {level,-5}  [{context}] {message}";

    private void Add(string level, string message, string? context)
    {
        var line = Format(_clock.Now, level, message, context);
        _lines.Insert(0, line);
        if (_lines.Count > MaxLines) _lines.RemoveAt(_lines.Count - 1);
        LineAdded?.Invoke(line);
    }
}
