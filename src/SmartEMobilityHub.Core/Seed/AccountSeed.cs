using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Services;

namespace SmartEMobilityHub.Core.Seed;

/// <summary>
/// Tài khoản demo dùng chung cho hai app. Đây là dữ liệu giả cho buổi trình bày,
/// không phải tài khoản thật; mật khẩu chỉ nằm trong mã nguồn demo này.
/// </summary>
public static class AccountSeed
{
    public const string StudentUsername = "2310001";
    public const string StudentPassword = "sv123456";
    public const string OperatorUsername = "op01";
    public const string OperatorPassword = "op123456";

    public static void Apply(AuthService auth)
    {
        auth.Register(StudentUsername, StudentPassword, "Minh", AccountRole.Student, StudentUsername);
        auth.Register(OperatorUsername, OperatorPassword, "Hoàng Vỹ", AccountRole.Operator, "OP-01");
    }
}
