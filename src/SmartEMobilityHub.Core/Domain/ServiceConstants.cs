namespace SmartEMobilityHub.Core.Domain;

/// <summary>
/// Hằng số nghiệp vụ — chỉ lấy từ NFR 2.2 và giả định A1–A6 của PLAN, không tự đặt số mới.
/// </summary>
public static class ServiceConstants
{
    /// <summary>NFR 2.2.1 — chu kỳ quét 30 giây.</summary>
    public const int ScanIntervalMs = 30_000;

    /// <summary>NFR 2.2.3 — dữ liệu cũ sau 10 phút.</summary>
    public const int StaleThresholdMinutes = 10;

    /// <summary>UC-SYS-04 — giữ chỗ tối đa 15 phút.</summary>
    public const int ReservationHoldMinutes = 15;

    /// <summary>UC-SYS-03 — ngưỡng quá tải 90%.</summary>
    public const double OverloadThreshold = 0.90;

    /// <summary>UC-SYS-03 Exception 4a — gửi lại sau 30 giây.</summary>
    public const int RetryDelayMs = 30_000;

    /// <summary>Giả định A1 — số lần gửi lại tối đa.</summary>
    public const int MaxRetry = 5;

    /// <summary>
    /// Giả định A10 (GĐ8) — số lần gia hạn tối đa khi người dùng đang quét QR (UC-SYS-04 5a).
    /// Sub 3 không giới hạn, nhưng nếu không chặn thì đơn có thể giữ xe mãi mãi.
    /// </summary>
    public const int MaxPickupExtensions = 5;

    /// <summary>Điều phối xe — Hub có tỷ lệ đỗ từ mức này trở lên là dư xe (giả định của nhánh điều phối).</summary>
    public const double DispatchHighLoadRatio = 0.85;

    /// <summary>Điều phối xe — Hub có tỷ lệ đỗ từ mức này trở xuống là thiếu xe.</summary>
    public const double DispatchLowLoadRatio = 0.35;

    /// <summary>Điều phối xe — tỷ lệ đỗ mục tiêu sau khi cân bằng.</summary>
    public const double DispatchTargetLoadRatio = 0.65;

    /// <summary>Điều phối xe — tốc độ di chuyển trung bình của xe điều phối (km/h).</summary>
    public const double DispatchAverageSpeedKmh = 25.0;

    /// <summary>Điều phối xe — thời gian di chuyển tối thiểu (phút), kể cả hai Hub sát nhau.</summary>
    public const double DispatchMinDurationMinutes = 3.0;

    /// <summary>UC-S-06 — chờ xác nhận mở khoá 30 giây.</summary>
    public const int UnlockTimeoutSeconds = 30;

    /// <summary>NFR 2.2.5 — giới hạn ảnh 5 MB.</summary>
    public const long MaxPhotoBytes = 5L * 1024 * 1024;

    /// <summary>Giả định A6 — công suất mỗi cổng 7 kW.</summary>
    public const double PowerPerPortKw = 7.0;

    /// <summary>Giả định A3 — nhu cầu cao điểm mặc định.</summary>
    public const double DefaultPeakDemand = 0.5;

    /// <summary>Giả định A2 — cửa sổ chuẩn hoá urgency (240 phút).</summary>
    public const double UrgencyWindowMinutes = 240.0;

    /// <summary>Hotline hỗ trợ hiển thị khi không mở khoá được (UC-S-06).</summary>
    public const string SupportHotline = "Hotline 1900-xxxx (demo)";

    /// <summary>Giả định A4 — SLA theo mức ưu tiên.</summary>
    public static TimeSpan SlaFor(PriorityLevel priority) => priority switch
    {
        PriorityLevel.Critical => TimeSpan.FromMinutes(30),
        PriorityLevel.High => TimeSpan.FromHours(2),
        PriorityLevel.Medium => TimeSpan.FromHours(8),
        _ => TimeSpan.FromHours(24)
    };
}
