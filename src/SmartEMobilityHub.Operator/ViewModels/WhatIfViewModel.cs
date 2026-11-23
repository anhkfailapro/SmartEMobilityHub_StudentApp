using System.Collections.ObjectModel;
using System.Globalization;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator.ViewModels;

public sealed record HubChoice(string Id, string Name);

public sealed class WhatIfViewModel : ObservableObject
{
    private readonly WhatIfController _controller;
    private readonly Func<DateTime> _now;
    private ScenarioPreset _selectedPreset = null!;
    private HubChoice _selectedHub = null!;
    private SimulationSession? _selectedHistorySession;
    private SimulationResult? _result;
    private string _demandSurgeText = "50";
    private double _demandSurgeValue = 50;
    private string _startTimeText = "14:00";
    private string _endTimeText = "19:00";
    private string _timeStepText = "30";
    private string _disabledChargerText = "1";
    private string _statusText = "Chọn kịch bản, chỉnh tham số rồi chạy mô phỏng.";
    private string _errorText = "";
    private string _currentSessionId = "";
    private string _scenarioLabel = "";
    private string _runLabel = "";
    private bool _isBusy;
    private bool _hasResult;

    public WhatIfViewModel(WhatIfController controller, NetworkStore store, Func<DateTime> now)
    {
        _controller = controller;
        _now = now;
        Presets = new ObservableCollection<ScenarioPreset>(_controller.GetAvailablePresets());
        Hubs = new ObservableCollection<HubChoice>(store.Hubs.Select(h => new HubChoice(h.Id, h.Name)));
        History = new ObservableCollection<SimulationSession>();
        Recommendations = new ObservableCollection<DispatchRecommendation>();
        Impacts = new ObservableCollection<HubImpact>();
        Forecast = new ObservableCollection<ForecastPoint>();
        _selectedPreset = Presets[0];
        _selectedHub = Hubs.FirstOrDefault(h => h.Id == "HUB-01") ?? Hubs[0];
        LoadPresetDefaults();
        RefreshHistory();
    }

    public ObservableCollection<ScenarioPreset> Presets { get; }
    public ObservableCollection<HubChoice> Hubs { get; }
    public ObservableCollection<SimulationSession> History { get; }
    public ObservableCollection<DispatchRecommendation> Recommendations { get; }
    public ObservableCollection<HubImpact> Impacts { get; }
    public ObservableCollection<ForecastPoint> Forecast { get; }

    public ScenarioPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (value is null || !SetProperty(ref _selectedPreset, value)) return;
            LoadPresetDefaults();
        }
    }

    public HubChoice SelectedHub
    {
        get => _selectedHub;
        set
        {
            if (value is null || !SetProperty(ref _selectedHub, value)) return;
            LoadPresetDefaults();
        }
    }

    public SimulationSession? SelectedHistorySession
    {
        get => _selectedHistorySession;
        set
        {
            if (!SetProperty(ref _selectedHistorySession, value) || value is null) return;
            CurrentSessionId = value.SessionId;
            SetResult(value.LatestResult);
            StatusText = $"Đang xem phiên {value.SessionId} · {value.Runs.Count} lần chạy.";
            ErrorText = "";
        }
    }

    public string DemandSurgeText
    {
        get => _demandSurgeText;
        set
        {
            if (!SetProperty(ref _demandSurgeText, value)) return;
            OnPropertyChanged(nameof(DemandSurgeValue));
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed) &&
                parsed is >= -50 and <= 200 && _demandSurgeValue != parsed)
            {
                _demandSurgeValue = parsed;
                OnPropertyChanged(nameof(DemandSurgeValue));
            }
        }
    }
    public double DemandSurgeValue
    {
        get => _demandSurgeValue;
        set
        {
            var rounded = Math.Clamp(Math.Round(value), -50, 200);
            if (!SetProperty(ref _demandSurgeValue, rounded)) return;
            DemandSurgeText = rounded.ToString("0", CultureInfo.CurrentCulture);
        }
    }
    public string StartTimeText { get => _startTimeText; set => SetProperty(ref _startTimeText, value); }
    public string EndTimeText { get => _endTimeText; set => SetProperty(ref _endTimeText, value); }
    public string TimeStepText { get => _timeStepText; set => SetProperty(ref _timeStepText, value); }
    public string DisabledChargerText { get => _disabledChargerText; set => SetProperty(ref _disabledChargerText, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string ErrorText { get => _errorText; private set => SetProperty(ref _errorText, value); }
    public string CurrentSessionId { get => _currentSessionId; private set => SetProperty(ref _currentSessionId, value); }
    public string ScenarioLabel { get => _scenarioLabel; private set => SetProperty(ref _scenarioLabel, value); }
    public string RunLabel { get => _runLabel; private set => SetProperty(ref _runLabel, value); }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanRun));
        }
    }
    public bool CanRun => !IsBusy;
    public bool HasResult { get => _hasResult; private set => SetProperty(ref _hasResult, value); }
    public double ForecastScale { get; private set; } = 2;
    public string ForecastAxisTop => ForecastScale.ToString("P0", CultureInfo.CurrentCulture);
    public string ForecastAxisUpper => (ForecastScale * 0.75).ToString("P0", CultureInfo.CurrentCulture);
    public string ForecastAxisMiddle => (ForecastScale * 0.5).ToString("P0", CultureInfo.CurrentCulture);
    public string ForecastAxisLower => (ForecastScale * 0.25).ToString("P0", CultureInfo.CurrentCulture);
    public string ForecastAxisBottom => "0%";

    public string Summary => _result?.Impact.Summary ?? "Chưa có kết quả mô phỏng.";
    public int AffectedHubCount => _result?.Impact.AffectedHubCount ?? 0;
    public int OverloadedHubCount => _result?.Impact.OverloadedHubCount ?? 0;
    public int CriticalHubCount => _result?.Impact.CriticalHubCount ?? 0;
    public string DurationText => _result is null ? "—" : $"{_result.Duration.TotalMilliseconds:0} ms";
    public string ResultTag => _result is null ? "" : _result.IsPostActionResult ? "KẾT QUẢ SAU ĐỀ XUẤT" : "DỰ BÁO WHAT-IF";
    public string ResultColor => _result is not null &&
        (_result.Impact.CriticalHubCount > 0 || _result.Impact.OverloadedHubCount > 0)
        ? "#B91C1C" : "#047857";

    public void RunSimulation()
    {
        ErrorText = "";
        if (!TryBuildParameters(out var parameters, out var error))
        {
            ErrorText = error;
            return;
        }

        var validation = _controller.ValidateParameters(SelectedPreset.Id, parameters);
        if (!validation.IsValid)
        {
            ErrorText = validation.Error ?? "Tham số chưa hợp lệ.";
            return;
        }

        IsBusy = true;
        try
        {
            var session = _controller.ConfigureScenario(SelectedPreset.Id, parameters);
            CurrentSessionId = session.SessionId;
            var result = _controller.StartSimulation(session.SessionId);
            SetResult(result);
            StatusText = $"Hoàn tất trong {DurationText}; snapshot được tách khỏi dữ liệu vận hành.";
            RefreshHistory(session.SessionId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or TimeoutException)
        {
            ErrorText = ex.Message;
            StatusText = "Mô phỏng chưa hoàn tất; không lưu kết quả lỗi.";
        }
        finally { IsBusy = false; }
    }

    public void ApplyRecommendation(string recommendationId)
    {
        if (string.IsNullOrWhiteSpace(CurrentSessionId)) return;
        ErrorText = "";
        IsBusy = true;
        try
        {
            var result = _controller.ApplyRecommendation(CurrentSessionId, recommendationId);
            SetResult(result);
            StatusText = $"Đã mô phỏng lại theo {recommendationId}; dữ liệu vận hành không bị thay đổi.";
            RefreshHistory(CurrentSessionId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or TimeoutException)
        {
            ErrorText = ex.Message;
        }
        finally { IsBusy = false; }
    }

    public void Rerun()
    {
        if (string.IsNullOrWhiteSpace(CurrentSessionId)) return;
        ErrorText = "";
        IsBusy = true;
        try
        {
            var result = _controller.RerunSimulation(CurrentSessionId);
            CurrentSessionId = result.SessionId;
            SetResult(result);
            StatusText = $"Đã tạo phiên mới {result.SessionId} từ cùng trạng thái đầu vào.";
            RefreshHistory(result.SessionId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or TimeoutException)
        {
            ErrorText = ex.Message;
        }
        finally { IsBusy = false; }
    }

    public byte[] ExportReport()
    {
        if (string.IsNullOrWhiteSpace(CurrentSessionId))
            throw new InvalidOperationException("Chưa có phiên mô phỏng để xuất báo cáo.");
        return _controller.ExportReportPdf(CurrentSessionId);
    }

    public void CancelConfiguration()
    {
        ErrorText = "";
        StatusText = "Thiết lập chưa lưu đã hủy.";
        SetResult(null);
    }

    private bool TryBuildParameters(out SimulationParameters parameters, out string error)
    {
        parameters = new SimulationParameters();
        error = "";
        if (!double.TryParse(DemandSurgeText, NumberStyles.Float, CultureInfo.CurrentCulture, out var surge))
        {
            error = "Mức tăng nhu cầu phải là số.";
            return false;
        }
        if (!int.TryParse(TimeStepText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var step))
        {
            error = "Bước mô phỏng phải là số phút.";
            return false;
        }
        if (!TimeSpan.TryParse(StartTimeText, CultureInfo.CurrentCulture, out var start) ||
            !TimeSpan.TryParse(EndTimeText, CultureInfo.CurrentCulture, out var end))
        {
            error = "Thời gian phải theo định dạng HH:mm.";
            return false;
        }
        if (start < TimeSpan.Zero || start >= TimeSpan.FromDays(1) ||
            end < TimeSpan.Zero || end >= TimeSpan.FromDays(1))
        {
            error = "Giờ mô phỏng phải nằm trong một ngày (00:00–23:59).";
            return false;
        }
        if (!int.TryParse(DisabledChargerText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var disabled))
        {
            error = "Số cổng sạc hỏng phải là số nguyên.";
            return false;
        }

        var date = _now().Date;
        parameters = new SimulationParameters
        {
            TargetHubId = SelectedHub.Id,
            DemandSurgePercent = surge,
            StartTime = date + start,
            EndTime = date + end,
            TimeStepMinutes = step,
            DisabledChargerCount = disabled
        };
        return true;
    }

    private void LoadPresetDefaults()
    {
        if (SelectedPreset is null || SelectedHub is null) return;
        var config = _controller.GetDefaultConfig(SelectedPreset.Id, SelectedHub.Id);
        DemandSurgeText = config.DemandSurgePercent.ToString("0.#", CultureInfo.CurrentCulture);
        StartTimeText = config.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        EndTimeText = config.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        TimeStepText = config.TimeStepMinutes.ToString(CultureInfo.InvariantCulture);
        DisabledChargerText = config.DisabledChargerCount.ToString(CultureInfo.InvariantCulture);
    }

    private void SetResult(SimulationResult? result)
    {
        _result = result;
        HasResult = result is not null;
        ScenarioLabel = result?.ScenarioName ?? "";
        RunLabel = result is null ? "" : $"{result.SessionId} · {result.RunId}";
        Recommendations.Clear();
        Impacts.Clear();
        Forecast.Clear();
        if (result is not null)
        {
            foreach (var recommendation in result.Recommendations) Recommendations.Add(recommendation);
            foreach (var point in result.Forecast) Forecast.Add(point);
            foreach (var impact in result.Impact.Hubs.Where(h => h.HubId == result.Parameters.TargetHubId ||
                         Math.Abs(h.ParkingUtilizationAfter - h.ParkingUtilizationBefore) > 0.0001 ||
                         h.ChargingPortsBusyAfter != h.ChargingPortsBusyBefore ||
                         h.AvailableVehiclesAfter != h.AvailableVehiclesBefore))
                Impacts.Add(impact);
        }
        var maximumForecast = Forecast.SelectMany(point => new[]
            { point.ParkingUtilization, point.ChargingUtilization }).DefaultIfEmpty(1).Max();
        ForecastScale = Math.Max(2, Math.Ceiling(maximumForecast * 2) / 2);
        OnPropertyChanged(nameof(ForecastScale));
        OnPropertyChanged(nameof(ForecastAxisTop));
        OnPropertyChanged(nameof(ForecastAxisUpper));
        OnPropertyChanged(nameof(ForecastAxisMiddle));
        OnPropertyChanged(nameof(ForecastAxisLower));
        OnPropertyChanged(nameof(ForecastAxisBottom));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(AffectedHubCount));
        OnPropertyChanged(nameof(OverloadedHubCount));
        OnPropertyChanged(nameof(CriticalHubCount));
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(ResultTag));
        OnPropertyChanged(nameof(ResultColor));
    }

    private void RefreshHistory(string? selectSessionId = null)
    {
        History.Clear();
        foreach (var session in _controller.GetHistory()) History.Add(session);
        if (selectSessionId is not null)
            SelectedHistorySession = History.FirstOrDefault(s => s.SessionId == selectSessionId);
    }
}
