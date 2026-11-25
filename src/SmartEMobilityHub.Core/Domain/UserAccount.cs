namespace SmartEMobilityHub.Core.Domain;

/// <summary>Loại người dùng được phép đăng nhập vào hệ thống.</summary>
public enum AccountRole
{
    /// <summary>Sinh viên dùng app di động.</summary>
    Student,

    /// <summary>Người vận hành dùng Trung tâm điều hành.</summary>
    Operator
}

/// <summary>
/// Tài khoản đăng nhập. Mật khẩu không lưu dạng rõ: chỉ giữ salt và hash PBKDF2.
/// Số lần sai và thời điểm hết khoá do <see cref="Services.AuthService"/> cập nhật.
/// </summary>
public sealed class UserAccount
{
    /// <summary>Tên đăng nhập (MSSV với sinh viên, mã đăng nhập với Operator). So khớp không phân biệt hoa thường.</summary>
    public string Username { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public AccountRole Role { get; init; }

    /// <summary>Mã liên kết sang dữ liệu nghiệp vụ, ví dụ OperatorId "OP-01" hoặc MSSV.</summary>
    public string LinkedId { get; init; } = "";

    public byte[] PasswordSalt { get; init; } = Array.Empty<byte>();

    public byte[] PasswordHash { get; init; } = Array.Empty<byte>();

    public int FailedAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }
}
