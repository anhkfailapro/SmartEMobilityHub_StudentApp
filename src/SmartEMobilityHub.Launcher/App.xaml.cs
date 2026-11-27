using System.Windows;

namespace SmartEMobilityHub.Launcher;

/// <summary>
/// Chế độ liên thông: mở app Sinh viên và cho Demo panel của nó mở Trung tâm điều hành trong cùng tiến trình,
/// trên cùng một phiên mạng lưới (DC-54). Đóng cửa sổ app Sinh viên thì thoát.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var student = new global::SmartEMobilityHub.MainWindow(new OperatorConsole());
        MainWindow = student;
        student.Show();
    }
}
