using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Tests.Ui.Support;

/// <summary>Hộp thoại giả: ghi lại cảnh báo / thông báo, trả lời xác nhận theo <see cref="ConfirmAnswer"/>.</summary>
public sealed class FakeDialogs : IDialogService
{
    public bool ConfirmAnswer { get; set; } = true;
    public List<(string Title, string Message)> Warnings { get; } = new();
    public List<(string Title, string Message)> Informs { get; } = new();
    public List<(string Title, string Message)> Confirms { get; } = new();

    public void Warn(string title, string message) => Warnings.Add((title, message));
    public void Inform(string title, string message) => Informs.Add((title, message));

    public bool Confirm(string title, string message)
    {
        Confirms.Add((title, message));
        return ConfirmAnswer;
    }
}
