namespace SmartEMobilityHub.Core.Domain;

/// <summary>Lỗi tham số — tương ứng IllegalArgumentException trong Bảng 31/37/41.</summary>
public sealed class IllegalArgumentException : Exception
{
    public IllegalArgumentException(string message) : base(message) { }
}

/// <summary>Lỗi trạng thái — tương ứng IllegalStateException trong Bảng 41/45.</summary>
public sealed class IllegalStateException : Exception
{
    public IllegalStateException(string message) : base(message) { }
}

/// <summary>Ảnh báo lỗi vượt 5 MB — Bảng 41 receiveStudentReport.</summary>
public sealed class FileTooLargeException : Exception
{
    public FileTooLargeException(string message) : base(message) { }
}

/// <summary>Không tìm thấy Hub — Bảng 41 suspendHub (UC-OP-03 Exception 1a).</summary>
public sealed class HubNotFoundException : Exception
{
    public HubNotFoundException(string message) : base(message) { }
}

/// <summary>Không tìm thấy sự cố — Bảng 41 lockDevice.</summary>
public sealed class IncidentNotFoundException : Exception
{
    public IncidentNotFoundException(string message) : base(message) { }
}

/// <summary>Gửi cảnh báo thất bại (mạng/timeout) — UC-SYS-03 Exception 4a.</summary>
public sealed class NotificationDeliveryException : Exception
{
    public NotificationDeliveryException(string message) : base(message) { }
}

/// <summary>Cảm biến không phản hồi — UC-SYS-02 Exception 2a.</summary>
public sealed class SensorUnreachableException : Exception
{
    public SensorUnreachableException(string message) : base(message) { }
}

/// <summary>Gateway quá thời gian chờ — UC-SYS-01 Exception 5a.</summary>
public sealed class GatewayTimeoutException : Exception
{
    public GatewayTimeoutException(string message) : base(message) { }
}
