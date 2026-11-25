using System.Globalization;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Data
{
    public static class DemoConfig
    {
        // Values marked "Assumed – review" in docs/DECISIONS.md (OPEN-08, OPEN-09, OPEN-10, OPEN-11).
        public const int RentalPricePerMinute = 1500;
        public const int DistanceFeePerKm = 0;
        public const int LateCancellationFee = 10000;
        public const int PrivateChargingTariffPerKWh = 3500;
        public const int IdleFeePerMinute = 1000;

        public const int HoldMinutes = 15;
        public const int LateCancelWindowMinutes = 5;
        public const int StaleMinutes = 10;
        public const int UnlockTimeoutSeconds = 30;
        /// <summary>Cùng giới hạn với Core (ServiceConstants.MaxPickupExtensions): không gia hạn giữ xe vô hạn.</summary>
        public const int MaxPickUpExtensions = 5;
        public const int UnlockNormalSeconds = 3;
        public const int MaxExtraPickUpAttempts = 2;
        public const double MaxPhotoMb = 5;
        public const int NeedsChargeBelow = 20;
        public const int ReturnExtraFreeMinutes = 10;
        public const int ReminderMinutesBefore = 30;
        public const int HistoryMonths = 3;
        public const int MaxActiveVehicle = 1;
        public const int MaxActiveParking = 1;
        public const int MaxActiveCharging = 1;
        public const double AlternativeHubRadiusKm = 1.0;
        public const int NearestHubCount = 3;
        public const string SupportHotline = "Hotline 1900-xxxx (demo)";
        public const string SupportHours = "7:00–22:00";
        public const string PaymentMethod = "Ví điện tử (demo)";

        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        public static string Money(int amount) => amount.ToString("N0", Vi) + "đ";

        public static string Km(double km) => km < 1 ? $"{Math.Round(km * 1000 / 10) * 10:0} m" : km.ToString("0.0", Vi) + " km";

        public static PriorityLevel PriorityFromScore(double score) =>
            score >= 75 ? PriorityLevel.Critical : score >= 50 ? PriorityLevel.High : score >= 25 ? PriorityLevel.Medium : PriorityLevel.Low;

        public static string PriorityLabel(PriorityLevel level) => level switch
        {
            PriorityLevel.Critical => "Khẩn cấp",
            PriorityLevel.High => "Cao",
            PriorityLevel.Medium => "Trung bình",
            _ => "Thấp"
        };

        public static (string Label, Tone Tone) VehicleStatusLabel(VehicleStatus status) => status switch
        {
            VehicleStatus.Available => ("Sẵn sàng", Tone.Success),
            VehicleStatus.Reserved => ("Đã đặt", Tone.Warning),
            VehicleStatus.InUse => ("Đang sử dụng", Tone.Info),
            VehicleStatus.NeedsCharge => ("Cần sạc", Tone.Warning),
            VehicleStatus.Charging => ("Đang sạc", Tone.Charging),
            VehicleStatus.Faulty => ("Hỏng", Tone.Danger),
            VehicleStatus.Relocating => ("Đang điều chuyển", Tone.Neutral),
            _ => ("Đang kiểm tra", Tone.Danger)
        };

        public static (string Label, Tone Tone) SessionStateLabel(SessionState state) => state switch
        {
            SessionState.Pending => ("Chờ kết nối", Tone.Neutral),
            SessionState.Charging => ("Đang sạc", Tone.Charging),
            SessionState.Completed => ("Hoàn thành", Tone.Success),
            SessionState.Failed => ("Lỗi/Bị ngắt", Tone.Danger),
            _ => ("Đã hủy", Tone.Neutral)
        };

        public static (string Label, Tone Tone) ReservationStateLabel(ReservationState state, bool vehicle) => state switch
        {
            ReservationState.Active => vehicle ? ("Chờ nhận", Tone.Warning) : ("Đã xác nhận", Tone.Info),
            ReservationState.Started => ("Đã bắt đầu", Tone.Info),
            ReservationState.Cancelled => ("Đã hủy", Tone.Neutral),
            ReservationState.Expired => ("Hết hạn", Tone.Neutral),
            _ => ("Hoàn tất", Tone.Success)
        };

        public static string Time(DateTime t) => t.ToString("HH:mm");

        public static string DateTimeShort(DateTime t) => t.ToString("dd/MM HH:mm");

        public static string Ago(DateTime then, DateTime now)
        {
            var d = now - then;
            if (d.TotalMinutes < 1) return "Vừa xong";
            if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} phút trước";
            if (d.TotalHours < 24) return $"{(int)d.TotalHours} giờ trước";
            if (d.TotalDays < 2) return "Hôm qua";
            return then.ToString("dd/MM/yyyy");
        }
    }
}
