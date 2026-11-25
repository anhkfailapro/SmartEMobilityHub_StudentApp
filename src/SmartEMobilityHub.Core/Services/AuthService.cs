using System.Security.Cryptography;
using System.Text;
using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;

namespace SmartEMobilityHub.Core.Services;

/// <summary>Lý do đăng nhập không thành công.</summary>
public enum LoginError
{
    None,
    MissingCredentials,
    InvalidCredentials,
    Locked,
    WrongRole
}

/// <summary>Kết quả một lần đăng nhập.</summary>
public sealed class LoginResult
{
    public bool Success => Error == LoginError.None;
    public LoginError Error { get; init; }
    public UserAccount? Account { get; init; }
    public string Message { get; init; } = "";

    /// <summary>Số lần thử còn lại trước khi bị khoá (chỉ có nghĩa khi sai mật khẩu).</summary>
    public int RemainingAttempts { get; init; }

    public DateTime? LockedUntil { get; init; }
}

/// <summary>
/// Đăng nhập bằng tài khoản lưu trong bộ nhớ (phục vụ demo, không có máy chủ thật).
/// - Mật khẩu băm PBKDF2-SHA256 có salt riêng, so sánh thời gian hằng.
/// - Sai 5 lần liên tiếp thì khoá 5 phút (theo IClock nên tua giờ demo được).
/// - Sai tên đăng nhập hay sai mật khẩu đều trả cùng một thông báo để không lộ tài khoản nào tồn tại.
/// </summary>
public sealed class AuthService
{
    public const int MaxFailedAttempts = 5;
    public const int LockoutMinutes = 5;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10_000;

    private readonly IClock _clock;
    private readonly Dictionary<string, UserAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);

    public AuthService(IClock clock) => _clock = clock;

    public UserAccount? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser is not null;

    public IReadOnlyCollection<UserAccount> Accounts => _accounts.Values;

    /// <summary>Thêm tài khoản mới; mật khẩu được băm ngay, không giữ bản rõ.</summary>
    public UserAccount Register(string username, string password, string displayName, AccountRole role, string linkedId = "")
    {
        var key = (username ?? "").Trim();
        if (key.Length == 0) throw new ArgumentException("Tên đăng nhập không được để trống.", nameof(username));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));
        if (_accounts.ContainsKey(key)) throw new InvalidOperationException($"Tài khoản {key} đã tồn tại.");

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var account = new UserAccount
        {
            Username = key,
            DisplayName = displayName,
            Role = role,
            LinkedId = linkedId,
            PasswordSalt = salt,
            PasswordHash = Hash(password, salt)
        };
        _accounts[key] = account;
        return account;
    }

    /// <summary>
    /// Đăng nhập. <paramref name="requiredRole"/> giới hạn loại tài khoản được vào app đang chạy
    /// (app Sinh viên chỉ nhận Student, Trung tâm điều hành chỉ nhận Operator).
    /// </summary>
    public LoginResult Login(string? username, string? password, AccountRole? requiredRole = null)
    {
        var key = (username ?? "").Trim();
        if (key.Length == 0 || string.IsNullOrEmpty(password))
            return Fail(LoginError.MissingCredentials, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");

        if (!_accounts.TryGetValue(key, out var account))
            return Fail(LoginError.InvalidCredentials, "Tên đăng nhập hoặc mật khẩu không đúng.");

        var now = _clock.Now;
        if (account.LockedUntil is { } until)
        {
            if (now < until)
                return new LoginResult
                {
                    Error = LoginError.Locked,
                    LockedUntil = until,
                    Message = $"Tài khoản tạm khoá do nhập sai {MaxFailedAttempts} lần. Thử lại sau {until:HH:mm}."
                };

            account.LockedUntil = null;
            account.FailedAttempts = 0;
        }

        if (!Verify(password, account))
        {
            account.FailedAttempts++;
            if (account.FailedAttempts >= MaxFailedAttempts)
            {
                account.LockedUntil = now.AddMinutes(LockoutMinutes);
                return new LoginResult
                {
                    Error = LoginError.Locked,
                    LockedUntil = account.LockedUntil,
                    Message = $"Nhập sai {MaxFailedAttempts} lần. Tài khoản tạm khoá {LockoutMinutes} phút (đến {account.LockedUntil:HH:mm})."
                };
            }

            var remaining = MaxFailedAttempts - account.FailedAttempts;
            return new LoginResult
            {
                Error = LoginError.InvalidCredentials,
                RemainingAttempts = remaining,
                Message = $"Tên đăng nhập hoặc mật khẩu không đúng. Còn {remaining} lần thử."
            };
        }

        if (requiredRole is { } role && account.Role != role)
            return Fail(LoginError.WrongRole, role == AccountRole.Operator
                ? "Tài khoản này không có quyền vào Trung tâm điều hành."
                : "Tài khoản này không dùng được trên app Sinh viên.");

        account.FailedAttempts = 0;
        CurrentUser = account;
        return new LoginResult { Error = LoginError.None, Account = account, Message = $"Xin chào, {account.DisplayName}." };
    }

    public void Logout() => CurrentUser = null;

    private static LoginResult Fail(LoginError error, string message) => new() { Error = error, Message = message };

    private static bool Verify(string password, UserAccount account) =>
        CryptographicOperations.FixedTimeEquals(Hash(password, account.PasswordSalt), account.PasswordHash);

    private static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
}
