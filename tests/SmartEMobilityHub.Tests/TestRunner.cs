namespace SmartEMobilityHub.Tests;

/// <summary>Kết quả một test để sinh báo cáo results.md/json ở GĐ5.</summary>
public sealed class TestResult
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool Passed { get; init; }
    public string Detail { get; init; } = "";
}

/// <summary>
/// Runner tối giản (không dùng NuGet — ràng buộc của nhóm: "No NuGet packages").
/// GĐ5 sẽ bổ sung xuất results.md/results.json từ danh sách kết quả này.
/// </summary>
public sealed class TestRunner
{
    private readonly List<(string Code, string Name, Action Body)> _cases = new();

    public void Add(string code, string name, Action body) => _cases.Add((code, name, body));

    public IReadOnlyList<TestResult> RunAll()
    {
        var results = new List<TestResult>();
        foreach (var (code, name, body) in _cases)
        {
            try
            {
                body();
                results.Add(new TestResult { Code = code, Name = name, Passed = true, Detail = "" });
                Console.WriteLine($"PASS  {code}  {name}");
            }
            catch (Exception ex)
            {
                results.Add(new TestResult
                {
                    Code = code,
                    Name = name,
                    Passed = false,
                    Detail = $"{ex.GetType().Name}: {ex.Message}"
                });
                Console.WriteLine($"FAIL  {code}  {name}");
                Console.WriteLine($"      {ex.GetType().Name}: {ex.Message}");
            }
        }

        var passed = results.Count(r => r.Passed);
        Console.WriteLine();
        Console.WriteLine($"{passed}/{results.Count} test đạt.");
        return results;
    }
}
