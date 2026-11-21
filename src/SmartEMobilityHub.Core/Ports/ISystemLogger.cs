namespace SmartEMobilityHub.Core.Ports;

/// <summary>Ghi log hệ thống (UC-SYS-02 Exception 2a, UC-SYS-03 Exception 4a).</summary>
public interface ISystemLogger
{
    void Info(string message, string? context = null);
    void Warn(string message, string? context = null);
    void Error(string message, string? context = null);
}
