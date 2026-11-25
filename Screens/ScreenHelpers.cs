using System.Windows;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;
using static SmartEMobilityHub.Ui.UiKit;

namespace SmartEMobilityHub.Screens
{
    public static class ScreenHelpers
    {
        public static string HubMeta(Hub h)
        {
            if (h.IsSuspended) return "Tạm dừng hoạt động · không nhận đặt xe, đỗ xe, sạc";
            var avail = DemoApp.Vehicles.AvailableCount(h);
            var slots = h.FreeSlots > 0 ? $"{h.FreeSlots} chỗ trống" : "Hết chỗ đỗ";
            return $"{avail} xe sẵn sàng · {slots} · {h.FreePoints} cổng sạc";
        }

        public static string DistanceFromMe(Hub h) => DemoConfig.Km(h.DistanceKmTo(0, 0));

        public static string Elapsed(TimeSpan e) => $"{(int)e.TotalHours:00}:{e.Minutes:00}:{e.Seconds:00}";

        public static string Duration(int minutes) => minutes % 60 == 0 ? $"{minutes / 60} giờ" : minutes > 60 ? $"{minutes / 60} giờ {minutes % 60} phút" : $"{minutes} phút";

        public static UIElement VehicleHeader(Vehicle v, string line)
        {
            var tile = IconTile(Icons.Bike, Tone.Info, 46);
            tile.Background = Theme.SkySubtle;
            return HStack(12, tile, Stack(2, Text(v.Code, TextKind.Subtitle), Text(line, TextKind.Caption, Theme.Text2)));
        }

        public static void GoHome() => DemoApp.GoTab(NavTab.Home);
    }
}
