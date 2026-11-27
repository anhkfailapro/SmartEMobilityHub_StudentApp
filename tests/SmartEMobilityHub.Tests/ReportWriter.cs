using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SmartEMobilityHub.Tests;

/// <summary>Xuất results.md + results.json sau mỗi lần chạy (GĐ5).</summary>
public static class ReportWriter
{
    public static string Write(IReadOnlyList<TestResult> results, string directory,
        string title = "Kết quả kiểm thử — Smart E-Mobility Hub (Core)",
        string source = "`tests/SmartEMobilityHub.Tests` (console runner, không dùng NuGet)")
    {
        Directory.CreateDirectory(directory);

        var runAt = DateTime.Now;
        var passed = results.Count(r => r.Passed);
        var failed = results.Count - passed;

        var markdown = new StringBuilder();
        markdown.AppendLine($"# {title}");
        markdown.AppendLine();
        markdown.AppendLine($"- Thời điểm chạy: {runAt:yyyy-MM-dd HH:mm:ss}");
        markdown.AppendLine($"- Tổng số: **{results.Count}** · Đạt: **{passed}** · Không đạt: **{failed}**");
        markdown.AppendLine($"- Nguồn: {source}");
        markdown.AppendLine();
        markdown.AppendLine("| Mã | Tên kịch bản | Kết quả | Chi tiết lỗi |");
        markdown.AppendLine("| --- | --- | --- | --- |");

        foreach (var result in results)
        {
            var detail = string.IsNullOrWhiteSpace(result.Detail) ? "—" : result.Detail.Replace("|", "\\|");
            markdown.AppendLine($"| {result.Code} | {result.Name} | {(result.Passed ? "PASS" : "FAIL")} | {detail} |");
        }

        if (failed > 0)
        {
            markdown.AppendLine();
            markdown.AppendLine("> Có kịch bản không đạt — xem cột chi tiết và sửa nguyên nhân, không bỏ qua test.");
        }

        File.WriteAllText(Path.Combine(directory, "results.md"), markdown.ToString(), new UTF8Encoding(false));

        var json = JsonSerializer.Serialize(
            new
            {
                runAt,
                total = results.Count,
                passed,
                failed,
                results
            },
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        var jsonPath = Path.Combine(directory, "results.json");
        File.WriteAllText(jsonPath, json, new UTF8Encoding(false));
        return jsonPath;
    }
}
