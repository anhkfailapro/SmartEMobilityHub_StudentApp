using System.Globalization;
using System.Text;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Store;

namespace SmartEMobilityHub.Core.Services;

/// <summary>Coordinates UC-WI-01..05 between the Operator UI, temp repository and simulation engine.</summary>
public sealed class WhatIfController
{
    private readonly TempStateRepository _repository;
    private readonly WhatIfEngineService _engine;
    private readonly Func<NetworkSnapshot> _currentSnapshot;
    private readonly IReadOnlyList<ScenarioPreset> _presets =
    [
        new("METRO_SURGE", "Sinh viên tăng đột biến tại ga Metro", "Tăng nhu cầu đi lại trong giờ cao điểm.", ScenarioType.MetroDemandSurge, 50),
        new("PARKING_FULL", "Hub hết chỗ đỗ", "Dự báo chỗ đỗ tại Hub mục tiêu bị lấp đầy.", ScenarioType.ParkingFull, 15),
        new("CHARGING_SURGE", "Nhu cầu sạc tăng cao", "Mô phỏng thêm xe cần sử dụng các cổng sạc.", ScenarioType.ChargingDemandSurge, 50),
        new("CHARGER_OUTAGE", "Một số cổng sạc bị hỏng", "Đưa số cổng sạc đã chọn về trạng thái offline trong snapshot.", ScenarioType.ChargerOutage, 0, 1),
        new("VEHICLE_CLUSTER", "Phương tiện tập trung tại một khu vực", "Điều chuyển xe khả dụng từ Hub khác tới Hub mục tiêu.", ScenarioType.VehicleConcentration, 50)
    ];

    public WhatIfController(
        TempStateRepository repository,
        WhatIfEngineService engine,
        Func<NetworkSnapshot> currentSnapshot)
    {
        _repository = repository;
        _engine = engine;
        _currentSnapshot = currentSnapshot;
    }

    public IReadOnlyList<ScenarioPreset> GetAvailablePresets() => _presets;

    public SimulationParameters GetDefaultConfig(string presetId, string targetHubId)
    {
        var preset = FindPreset(presetId);
        return preset.GetDefaultConfig(targetHubId);
    }

    public ValidationResult ValidateParameters(string presetId, SimulationParameters parameters)
    {
        var snapshot = _currentSnapshot();
        var preset = FindPreset(presetId);
        var common = _engine.ValidateParameters(preset, parameters, snapshot);
        return common.IsValid ? preset.ValidateCustomValues(parameters, snapshot) : common;
    }

    public SimulationSession ConfigureScenario(string presetId, SimulationParameters parameters)
    {
        var snapshot = _currentSnapshot();
        var preset = FindPreset(presetId);
        var validation = _engine.ValidateParameters(preset, parameters, snapshot);
        if (!validation.IsValid) throw new ArgumentException(validation.Error, nameof(parameters));
        var customValidation = preset.ValidateCustomValues(parameters, snapshot);
        if (!customValidation.IsValid) throw new ArgumentException(customValidation.Error, nameof(parameters));
        return _repository.CreateSession(preset, parameters);
    }

    public SimulationResult StartSimulation(string sessionId)
    {
        var session = _repository.GetSession(sessionId);
        var snapshot = _repository.CloneCurrentNetworkState(sessionId);
        session.State = SimulationState.Simulating;
        try
        {
            var result = _engine.ExecuteSimulation(sessionId, NextRunId(), session.Preset,
                snapshot, session.Parameters);
            session.State = SimulationState.Evaluating;
            _repository.SaveTemporaryResult(sessionId, result);
            return result;
        }
        catch
        {
            session.State = SimulationState.Failed;
            throw;
        }
    }

    public SimulationResult ApplyRecommendation(string sessionId, string recommendationId)
    {
        var session = _repository.GetSession(sessionId);
        var baseline = session.LatestResult
                       ?? throw new InvalidOperationException("Chạy mô phỏng trước khi áp dụng đề xuất.");
        var recommendation = baseline.Recommendations.FirstOrDefault(r => r.Id == recommendationId)
                            ?? throw new KeyNotFoundException("Không tìm thấy phương án trong kết quả mới nhất.");
        session.State = SimulationState.ReSimulating;
        try
        {
            var result = _engine.ReSimulateWithAction(sessionId, NextRunId(), session, baseline, recommendation);
            _repository.SaveTemporaryResult(sessionId, result);
            return result;
        }
        catch
        {
            session.State = SimulationState.Failed;
            throw;
        }
    }

    public SimulationResult RerunSimulation(string sessionId)
    {
        var rerun = _repository.CreateRerunSession(sessionId);
        var snapshot = _repository.CloneCurrentNetworkState(rerun.SessionId);
        var result = _engine.ExecuteSimulation(rerun.SessionId, NextRunId(), rerun.Preset,
            snapshot, rerun.Parameters);
        _repository.SaveTemporaryResult(rerun.SessionId, result);
        return result;
    }

    public void CancelSimulation(string sessionId) => _repository.DeleteSnapshot(sessionId);

    public SimulationSession GetSession(string sessionId) => _repository.GetSession(sessionId);

    public IReadOnlyList<SimulationSession> GetHistory() => _repository.GetHistory();

    public byte[] ExportReportPdf(string sessionId)
    {
        var session = _repository.GetSession(sessionId);
        var result = session.LatestResult
                      ?? throw new InvalidOperationException("Phiên chưa có kết quả để xuất báo cáo.");
        var lines = new List<string>
        {
            "SMART E-MOBILITY HUB - WHAT-IF SIMULATION REPORT",
            $"Session: {session.SessionId} | Run: {result.RunId}",
            $"Scenario: {result.ScenarioName}",
            $"Hub: {result.Parameters.TargetHubId} | Demand: {result.Parameters.DemandSurgePercent:+0;-0;0}%",
            $"Window: {result.Parameters.StartTime:yyyy-MM-dd HH:mm} - {result.Parameters.EndTime:HH:mm}",
            $"Run time: {result.Duration.TotalMilliseconds:0} ms | Post-action: {result.IsPostActionResult}",
            "",
            "IMPACT SUMMARY",
            Ascii(result.Impact.Summary),
            $"Affected hubs: {result.Impact.AffectedHubCount} | At/above 90%: {result.Impact.OverloadedHubCount} | Above 100%: {result.Impact.CriticalHubCount}",
            "",
            "HUB COMPARISON"
        };
        lines.AddRange(result.Impact.Hubs.Where(h => h.HubId == result.Parameters.TargetHubId ||
                                                    Math.Abs(h.ParkingUtilizationAfter - h.ParkingUtilizationBefore) > 0.0001 ||
                                                    h.ChargingPortsBusyAfter != h.ChargingPortsBusyBefore)
            .Select(h => $"{Ascii(h.HubName)}: parking {h.OccupiedBefore}/{h.Capacity} ({h.ParkingUtilizationBefore:P0})->{h.OccupiedAfter}/{h.Capacity} ({h.ParkingUtilizationAfter:P0}); charging {h.ChargingUtilizationBefore:P0}->{h.ChargingUtilizationAfter:P0}; vehicles {h.AvailableVehiclesBefore}->{h.AvailableVehiclesAfter}; wait {h.EstimatedChargingWaitBeforeMinutes}->{h.EstimatedChargingWaitAfterMinutes} min."));
        lines.Add("");
        lines.Add("TIME-STEP FORECAST");
        lines.AddRange(result.Forecast.Select(p =>
            $"{p.At:HH:mm}: parking {p.OccupiedSlots}/{p.Capacity} ({p.ParkingUtilization:P0}), charging {p.ChargingUtilization:P0}."));
        lines.Add("");
        lines.Add("RECOMMENDATIONS");
        lines.AddRange(result.Recommendations.Select((r, i) => $"{i + 1}. {Ascii(r.Title)} - {Ascii(r.Description)} Reason: {Ascii(r.Reason)}"));
        lines.Add("");
        lines.Add("Simulation uses an isolated snapshot. No operational state was changed.");
        var pdf = BuildPdf(lines);
        _repository.CompleteSession(sessionId);
        return pdf;
    }

    private ScenarioPreset FindPreset(string presetId) =>
        _presets.FirstOrDefault(p => string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Không tìm thấy kịch bản {presetId}.");

    private static string NextRunId() => $"RUN-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    private static string Ascii(string value)
    {
        var decomposed = value.Replace("đ", "d", StringComparison.OrdinalIgnoreCase)
            .Replace("Đ", "D", StringComparison.OrdinalIgnoreCase)
            .Normalize(NormalizationForm.FormD);
        var result = new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => c <= 127 ? c : ' ').ToArray()).Normalize(NormalizationForm.FormC);
        return result.Replace("\r", " ").Replace("\n", " ");
    }

    // Minimal one-page PDF using built-in Helvetica; keeping the MVP package-free.
    private static byte[] BuildPdf(IEnumerable<string> content)
    {
        var lines = content.Select(Ascii).Take(52).Select(s => s.Length > 105 ? s[..102] + "..." : s).ToList();
        var stream = new StringBuilder("BT /F1 9 Tf 42 800 Td 12 TL\n");
        foreach (var line in lines)
        {
            stream.Append('(').Append(line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"))
                .Append(") Tj T* \n");
        }
        stream.Append("ET");
        var streamBytes = Encoding.ASCII.GetBytes(stream.ToString());
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {streamBytes.Length} >>\nstream\n{stream}\nendstream"
        };
        using var output = new MemoryStream();
        void WriteAscii(string value) => output.Write(Encoding.ASCII.GetBytes(value));
        WriteAscii("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(output.Position);
            WriteAscii($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xrefOffset = output.Position;
        WriteAscii($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii($"{offset:0000000000} 00000 n \n");
        WriteAscii($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return output.ToArray();
    }
}
