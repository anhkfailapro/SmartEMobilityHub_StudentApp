using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Ports;

/// <summary>Bước 4 của StatusUpdaterTimer: lập lịch sạc cho một Hub ONLINE (UC-SYS-01).</summary>
public interface ISmartSchedulingService
{
    void TriggerFor(MobilityHub hub);
}
