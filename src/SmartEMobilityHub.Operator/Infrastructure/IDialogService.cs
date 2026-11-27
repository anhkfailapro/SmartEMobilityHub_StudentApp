using System.Windows;

namespace SmartEMobilityHub.Operator.Infrastructure;

/// <summary>Hộp thoại mà ViewModel cần (cảnh báo, xác nhận). Cửa sổ cung cấp bản thật; chụp màn hình/test để trống.</summary>
public interface IDialogService
{
    void Warn(string title, string message);
    void Inform(string title, string message);
    bool Confirm(string title, string message);
}

/// <summary>Hộp thoại MessageBox gắn với một cửa sổ chủ.</summary>
public sealed class WindowDialogService : IDialogService
{
    private readonly Window _owner;

    public WindowDialogService(Window owner) => _owner = owner;

    public void Warn(string title, string message) =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void Inform(string title, string message) =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string title, string message) =>
        MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
