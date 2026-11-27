using System.Diagnostics;
using System.Text;
using System.Windows;

namespace SmartEMobilityHub.Tests.Ui.Support;

/// <summary>
/// Gom lỗi binding WPF (đường dẫn sai, thuộc tính không tồn tại…) mà bình thường chỉ hiện ở cửa sổ Output của Visual Studio.
/// Trình biên dịch không kiểm tra binding trong XAML, nên đây là lưới an toàn khi đổi tên/tách ViewModel.
/// Chỉ giữ lỗi "BindingExpression path error" (thuộc tính không có trên DataContext); bỏ các cảnh báo nền
/// quen thuộc của WPF (vd. ComboBoxItem tìm HorizontalContentAlignment) vốn không liên quan tới ViewModel.
/// </summary>
public sealed class BindingErrors : TraceListener
{
    private readonly StringBuilder _pending = new();

    private BindingErrors()
    {
    }

    public List<string> Errors { get; } = new();

    public static BindingErrors Attach()
    {
        var listener = new BindingErrors();
        PresentationTraceSources.Refresh();
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        return listener;
    }

    public void Detach() => PresentationTraceSources.DataBindingSource.Listeners.Remove(this);

    public override void Write(string? message) => _pending.Append(message);

    public override void WriteLine(string? message)
    {
        _pending.Append(message);
        var line = _pending.ToString();
        _pending.Clear();
        if (line.Contains("BindingExpression path error", StringComparison.Ordinal)) Errors.Add(line);
    }
}
