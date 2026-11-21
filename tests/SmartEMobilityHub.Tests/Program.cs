using SmartEMobilityHub.Tests;
using SmartEMobilityHub.Tests.Unit;
using SmartEMobilityHub.Tests.UseCase;

var runner = new TestRunner();
SeedSmokeTests.Register(runner);
TCSYS02Tests.Register(runner);
TCSYS03Tests.Register(runner);
TCSYS04Tests.Register(runner);
TCSYS01Tests.Register(runner);
TCOP03Tests.Register(runner);
BoundaryTests.Register(runner);

Console.WriteLine("Smart E-Mobility Hub — Core test runner");
Console.WriteLine(new string('-', 60));

var results = runner.RunAll();
var reportDirectory = Path.Combine(FindRepoRoot(), "tests", "report");
var jsonPath = ReportWriter.Write(results, reportDirectory);

Console.WriteLine();
Console.WriteLine($"Báo cáo: {reportDirectory}{Path.DirectorySeparatorChar}results.md");
Console.WriteLine($"         {jsonPath}");

return results.All(r => r.Passed) ? 0 : 1;

/// <summary>Tìm thư mục gốc repo để báo cáo luôn rơi đúng tests/report.</summary>
static string FindRepoRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SmartEMobilityHub.slnx")))
        directory = directory.Parent;

    return directory?.FullName ?? Directory.GetCurrentDirectory();
}
