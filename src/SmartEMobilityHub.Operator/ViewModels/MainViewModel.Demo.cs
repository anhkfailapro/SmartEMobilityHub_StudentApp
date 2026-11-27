using System.Collections.ObjectModel;
using System.Windows;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Operator.Models;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>Trang "Kịch bản demo &amp; nhật ký": các nút bơm sự kiện IoT, mạng cảnh báo, yêu cầu sạc mẫu.</summary>
public sealed partial class MainViewModel
{
    public void BreakPort()
    {
        _demo.BreakChargingPoint("CS-08-08", "HUB-08");
        Note("Đã bơm lỗi cổng CS-08-08 · sinh viên/cảm biến đã tạo sự cố.");
        Refresh();
    }

    public void DisconnectHub()
    {
        _demo.DisconnectHub("HUB-03");
        Note("Đã ngắt cảm biến HUB-03 · cả 4 cổng chuyển OFFLINE.");
        Refresh();
    }

    public void MakeStale()
    {
        _demo.MakeStale("HUB-08");
        Note("Dữ liệu HUB-08 đã cũ 14 phút · cảnh báo STALE_DATA đã phát.");
        Refresh();
    }

    public void BreakChannel()
    {
        var pending = _demo.BreakAlertChannel();
        Note($"Mạng cảnh báo đang lỗi · {pending} cảnh báo nằm trong hàng đợi gửi lại.");
        Refresh();
    }

    public void RestoreChannel()
    {
        var sent = _demo.RestoreAlertChannel();
        Note($"Đã khôi phục mạng · gửi lại thành công {sent} cảnh báo.");
        Refresh();
    }

    public void RaiseUnlockIncident()
    {
        var record = _demo.RaiseUnlockTimeout();
        Note($"Đã tạo {record.IncidentId} · xe XE-121 giữ RESERVED, đơn R-103 bị huỷ.");
        Refresh();
    }

    public void AddChargingRequests()
    {
        var ports = _demo.AddChargingRequests();
        ChargingHubId = OperatorDemo.ChargingDemoHubId;
        Note($"Đã lập lịch sạc {OperatorDemo.ChargingDemoHubId} · cấp {ports} cổng trong giới hạn công suất.");
        Refresh();
    }
}
