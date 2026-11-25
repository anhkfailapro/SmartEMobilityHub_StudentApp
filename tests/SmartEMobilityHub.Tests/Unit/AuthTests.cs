using SmartEMobilityHub.Core.Clock;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Core.Seed;
using SmartEMobilityHub.Core.Services;

namespace SmartEMobilityHub.Tests.Unit;

/// <summary>Đăng nhập / đăng xuất cho app Sinh viên và Trung tâm điều hành — AUTH-01..08.</summary>
public static class AuthTests
{
    private static (AuthService Auth, FakeClock Clock) Create()
    {
        var clock = new FakeClock(new DateTime(2026, 11, 21, 9, 0, 0));
        var auth = new AuthService(clock);
        AccountSeed.Apply(auth);
        return (auth, clock);
    }

    public static void Register(TestRunner runner)
    {
        runner.Add("AUTH-01", "Đúng MSSV và mật khẩu thì đăng nhập được vào app Sinh viên", () =>
        {
            var (auth, _) = Create();
            var result = auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword, AccountRole.Student);
            Assert.True(result.Success, "Tài khoản demo sinh viên phải đăng nhập được");
            Assert.True(auth.IsLoggedIn, "Phải có người dùng hiện tại");
            Assert.Equal("Minh", auth.CurrentUser!.DisplayName, "Tên hiển thị");
        });

        runner.Add("AUTH-02", "Bỏ trống ô nào cũng bị từ chối, không tính là lần sai", () =>
        {
            var (auth, _) = Create();
            Assert.Equal(LoginError.MissingCredentials, auth.Login("", AccountSeed.StudentPassword).Error, "Thiếu tên đăng nhập");
            Assert.Equal(LoginError.MissingCredentials, auth.Login("   ", "x").Error, "Tên chỉ có khoảng trắng");
            Assert.Equal(LoginError.MissingCredentials, auth.Login(AccountSeed.StudentUsername, "").Error, "Thiếu mật khẩu");
            Assert.Equal(0, auth.Accounts.First(a => a.Username == AccountSeed.StudentUsername).FailedAttempts, "Không cộng lần sai");
        });

        runner.Add("AUTH-03", "Sai mật khẩu và sai tài khoản trả cùng một lỗi, báo số lần thử còn lại", () =>
        {
            var (auth, _) = Create();
            var wrongPwd = auth.Login(AccountSeed.StudentUsername, "sai-mat-khau");
            var unknown = auth.Login("9999999", "bat-ky");
            Assert.Equal(LoginError.InvalidCredentials, wrongPwd.Error, "Sai mật khẩu");
            Assert.Equal(LoginError.InvalidCredentials, unknown.Error, "Tài khoản không tồn tại");
            Assert.Equal(AuthService.MaxFailedAttempts - 1, wrongPwd.RemainingAttempts, "Còn 4 lần");
            Assert.False(auth.IsLoggedIn, "Không được đăng nhập");
        });

        runner.Add("AUTH-04", "Sai 5 lần thì khoá 5 phút, kể cả nhập đúng mật khẩu trong thời gian khoá", () =>
        {
            var (auth, clock) = Create();
            LoginResult last = new();
            for (var i = 0; i < AuthService.MaxFailedAttempts; i++) last = auth.Login(AccountSeed.StudentUsername, "sai");
            Assert.Equal(LoginError.Locked, last.Error, "Lần sai thứ 5 phải khoá");

            clock.AdvanceMinutes(AuthService.LockoutMinutes - 1);
            Assert.Equal(LoginError.Locked, auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword).Error, "Còn trong thời gian khoá");

            clock.AdvanceMinutes(1);
            Assert.True(auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword).Success, "Hết khoá thì đăng nhập được");
        });

        runner.Add("AUTH-05", "Đăng nhập đúng thì xoá đếm lần sai", () =>
        {
            var (auth, _) = Create();
            auth.Login(AccountSeed.OperatorUsername, "sai");
            auth.Login(AccountSeed.OperatorUsername, "sai");
            Assert.True(auth.Login(AccountSeed.OperatorUsername, AccountSeed.OperatorPassword).Success, "Đúng mật khẩu");
            var again = auth.Login(AccountSeed.OperatorUsername, "sai");
            Assert.Equal(AuthService.MaxFailedAttempts - 1, again.RemainingAttempts, "Đếm lại từ đầu");
        });

        runner.Add("AUTH-06", "Tài khoản sinh viên không vào được Trung tâm điều hành và ngược lại", () =>
        {
            var (auth, _) = Create();
            Assert.Equal(LoginError.WrongRole,
                auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword, AccountRole.Operator).Error, "SV vào app Operator");
            Assert.Equal(LoginError.WrongRole,
                auth.Login(AccountSeed.OperatorUsername, AccountSeed.OperatorPassword, AccountRole.Student).Error, "Operator vào app SV");
            Assert.False(auth.IsLoggedIn, "Sai quyền thì không tạo phiên");

            var ok = auth.Login(AccountSeed.OperatorUsername, AccountSeed.OperatorPassword, AccountRole.Operator);
            Assert.Equal("OP-01", ok.Account!.LinkedId, "Liên kết đúng Operator trong seed");
        });

        runner.Add("AUTH-07", "Tên đăng nhập bỏ khoảng trắng, không phân biệt hoa thường; mật khẩu thì phân biệt", () =>
        {
            var (auth, _) = Create();
            Assert.True(auth.Login("  OP01 ", AccountSeed.OperatorPassword).Success, "Tên viết hoa, có khoảng trắng");
            auth.Logout();
            Assert.False(auth.Login(AccountSeed.OperatorUsername, AccountSeed.OperatorPassword.ToUpperInvariant()).Success, "Mật khẩu viết hoa");
        });

        runner.Add("AUTH-08", "Mật khẩu không lưu dạng rõ; đăng xuất thì hết phiên; không tạo trùng tài khoản", () =>
        {
            var (auth, _) = Create();
            foreach (var account in auth.Accounts)
            {
                Assert.True(account.PasswordHash.Length == 32 && account.PasswordSalt.Length == 16, "Có hash và salt");
                Assert.False(System.Text.Encoding.UTF8.GetString(account.PasswordHash).Contains("123456"), "Không chứa mật khẩu rõ");
            }

            auth.Login(AccountSeed.StudentUsername, AccountSeed.StudentPassword);
            auth.Logout();
            Assert.False(auth.IsLoggedIn, "Đăng xuất xong không còn phiên");
            Assert.Throws<InvalidOperationException>(
                () => auth.Register(AccountSeed.StudentUsername, "x", "Trùng", AccountRole.Student), "Trùng tài khoản");
        });
    }
}
