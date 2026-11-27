using System.Windows;
using SmartEMobilityHub.Models;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub.Screens
{
    /// <summary>
    /// Một màn hình của app Sinh viên. Màn hình nhận <see cref="DemoApp"/> qua constructor và đọc dữ liệu, service,
    /// khung điều hướng từ đó (không đọc trạng thái toàn cục), nên test dựng màn hình trên một app riêng được.
    /// </summary>
    public abstract class Screen
    {
        protected Screen(DemoApp app)
        {
            App = app;
        }

        /// <summary>App Sinh viên đang chạy (dữ liệu, service, đồng hồ demo, khung điều hướng).</summary>
        protected DemoApp App { get; }

        protected AppShell Shell => App.Shell;

        public virtual string? Title => null;
        public virtual bool ShowBack => true;
        public virtual NavTab? Tab => null;
        public virtual bool WantsTick => false;

        public abstract UIElement BuildContent();

        public virtual UIElement? BuildActions() => null;

        public virtual void OnBack() => Shell.Back();

        public virtual void OnTick()
        {
        }

        protected void GoHome() => App.GoTab(NavTab.Home);

        /// <summary>Dòng tóm tắt dưới tên Hub: xe sẵn sàng, chỗ đỗ, cổng sạc (hoặc trạng thái tạm dừng).</summary>
        protected string HubMeta(Hub h)
        {
            if (h.IsSuspended) return "Tạm dừng hoạt động · không nhận đặt xe, đỗ xe, sạc";
            var avail = App.Vehicles.AvailableCount(h);
            var slots = h.FreeSlots > 0 ? $"{h.FreeSlots} chỗ trống" : "Hết chỗ đỗ";
            return $"{avail} xe sẵn sàng · {slots} · {h.FreePoints} cổng sạc";
        }
    }
}
