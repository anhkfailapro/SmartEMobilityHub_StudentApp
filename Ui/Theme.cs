using System.Windows.Media;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    public static class Theme
    {
        private static readonly Dictionary<string, SolidColorBrush> Cache = new();

        public static bool IsDark { get; set; }

        private static SolidColorBrush B(string light, string dark)
        {
            var hex = IsDark ? dark : light;
            if (!Cache.TryGetValue(hex, out var brush))
            {
                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                Cache[hex] = brush;
            }
            return brush;
        }

        public static SolidColorBrush CanvasBg => B("#F8FAFC", "#020617");
        public static SolidColorBrush Surface => B("#FFFFFF", "#0F172A");
        public static SolidColorBrush SurfaceAlt => B("#F1F5F9", "#1E293B");
        public static SolidColorBrush Text => B("#0F172A", "#F8FAFC");
        public static SolidColorBrush Text2 => B("#64748B", "#94A3B8");
        public static SolidColorBrush OnPrimary => B("#FFFFFF", "#FFFFFF");
        public static SolidColorBrush Border => B("#E2E8F0", "#334155");
        public static SolidColorBrush BorderStrong => B("#CBD5E1", "#475569");
        public static SolidColorBrush Primary => B("#2563EB", "#3B82F6");
        public static SolidColorBrush PrimarySubtle => B("#EFF6FF", "#1E3A8A");
        public static SolidColorBrush Danger => B("#DC2626", "#EF4444");
        public static SolidColorBrush DangerSubtle => B("#FEF2F2", "#7F1D1D");
        public static SolidColorBrush Success => B("#10B981", "#10B981");
        public static SolidColorBrush SuccessSubtle => B("#D1FAE5", "#064E3B");
        public static SolidColorBrush Warning => B("#F59E0B", "#F59E0B");
        public static SolidColorBrush WarningSubtle => B("#FEF3C7", "#78350F");
        public static SolidColorBrush Charging => B("#8B5CF6", "#8B5CF6");
        public static SolidColorBrush ChargingSubtle => B("#EDE9FE", "#4C1D95");
        public static SolidColorBrush Sky => B("#0EA5E9", "#0EA5E9");
        public static SolidColorBrush SkySubtle => B("#E0F2FE", "#0C4A6E");

        public static (SolidColorBrush Background, SolidColorBrush Foreground) ToneBrushes(Tone tone) => tone switch
        {
            Tone.Success => (SuccessSubtle, Success),
            Tone.Warning => (WarningSubtle, Warning),
            Tone.Danger => (DangerSubtle, Danger),
            Tone.Charging => (ChargingSubtle, Charging),
            Tone.Neutral => (SurfaceAlt, Text2),
            _ => (PrimarySubtle, Primary)
        };
    }
}
