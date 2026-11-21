namespace SmartEMobilityHub.Core.Ports;

/// <summary>
/// Nguồn lập lịch của StatusUpdaterTimer. Adapter WPF dùng DispatcherTimer,
/// adapter test dùng lịch thủ công — nhờ đó test 30 giây không phải chờ thật.
/// </summary>
public interface IScheduler
{
    /// <summary>Chạy <paramref name="onTick"/> mỗi <paramref name="interval"/>; trả handle để huỷ.</summary>
    IDisposable Schedule(TimeSpan interval, Action onTick);
}
