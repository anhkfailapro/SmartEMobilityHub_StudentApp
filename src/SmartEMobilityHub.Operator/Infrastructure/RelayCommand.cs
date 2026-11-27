using System.Windows.Input;

namespace SmartEMobilityHub.Operator.Infrastructure;

/// <summary>
/// ICommand tối giản cho nút bấm (không dùng NuGet — DC-24). Nút vẫn bật/tắt bằng các thuộc tính Can… đã bind
/// vào IsEnabled; lệnh chỉ thay cho handler Click trong code-behind (DC-52).
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;

    public RelayCommand(Action execute) : this(_ => execute())
    {
    }

    public RelayCommand(Action<object?> execute) => _execute = execute;

    // Luôn thực thi được; trạng thái bật/tắt do IsEnabled quyết định nên không cần phát sự kiện này.
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);
}
