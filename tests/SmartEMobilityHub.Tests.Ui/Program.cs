using System.Windows;
using SmartEMobilityHub.Tests;
using SmartEMobilityHub.Tests.Ui;
using SmartEMobilityHub.Tests.Ui.Support;

namespace SmartEMobilityHub.Tests.Ui;

/// <summary>
/// Test tầng giao diện WPF (cần Windows). Chạy: dotnet run --project tests/SmartEMobilityHub.Tests.Ui
/// Báo cáo ghi ở tests/report/ui (results.md, results.json), kèm độ phủ màn hình app Sinh viên.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main()
    {
        // Cửa sổ Operator nạp bảng màu vào Application hiện tại; không tự thoát khi đóng cửa sổ test.
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

        var runner = new TestRunner();
        OperatorUiTests.Register(runner);
        StudentUiTests.Register(runner);

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Smart E-Mobility Hub — test giao diện (ViewModel Operator, cửa sổ, màn hình app Sinh viên)");
        Console.WriteLine(new string('-', 60));

        var results = runner.RunAll();
        var all = ScreenCoverage.AllScreens.Count;
        var rendered = all - ScreenCoverage.Missing.Count;
        Console.WriteLine($"Độ phủ màn hình app Sinh viên: {rendered}/{all} lớp Screen đã dựng.");

        var directory = Path.Combine(FindRepoRoot(), "tests", "report", "ui");
        ReportWriter.Write(results, directory,
            title: $"Kết quả kiểm thử giao diện — Smart E-Mobility Hub (độ phủ màn hình {rendered}/{all})",
            source: "`tests/SmartEMobilityHub.Tests.Ui` (console runner WPF, không dùng NuGet)");
        Console.WriteLine($"Báo cáo: {directory}{Path.DirectorySeparatorChar}results.md");

        application.Shutdown();
        return results.All(r => r.Passed) ? 0 : 1;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SmartEMobilityHub.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}
