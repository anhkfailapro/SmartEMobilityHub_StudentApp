using System.Windows;
using SmartEMobilityHub.Core;
using CoreServices = SmartEMobilityHub.Core.Services;
using OperatorDemo = SmartEMobilityHub.Operator.OperatorDemo;
using OperatorLoginWindow = SmartEMobilityHub.Operator.LoginWindow;
using OperatorMainWindow = SmartEMobilityHub.Operator.MainWindow;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Mở Trung tâm điều hành trong cùng tiến trình, trên CHÍNH phiên mạng lưới của app Sinh viên (DC-54).
    /// Trước đây lớp này đẩy thao tác của sinh viên sang Core rồi mỗi giây đọc ngược thay đổi về một kho dữ liệu riêng;
    /// giờ hai app dùng chung một <see cref="NetworkSession"/> nên không còn gì phải đồng bộ.
    /// Thông báo cho sinh viên về thay đổi do Operator tạo ra nằm ở <see cref="NetworkWatcher"/>.
    /// </summary>
    public sealed class NetworkLink
    {
        private OperatorMainWindow? _window;
        private CoreServices.AuthService? _operatorAuth;
        private OperatorDemo? _demo;
        private NetworkSession? _demoSession;

        public bool IsOperatorWindowOpen => _window is not null;

        /// <summary>Trung tâm điều hành của phiên mạng lưới hiện tại (tạo khi cần, giữ nhật ký giữa các lần mở cửa sổ).</summary>
        public OperatorDemo Demo
        {
            get
            {
                var session = DemoApp.Context.Network;
                if (_demo is null || !ReferenceEquals(_demoSession, session))
                {
                    _demo = new OperatorDemo(session);
                    _demoSession = session;
                }
                return _demo;
            }
        }

        /// <summary>Gọi sau khi app Sinh viên đặt lại demo: cửa sổ Operator đang mở chuyển sang mạng lưới mới.</summary>
        public void Reset()
        {
            _window?.AttachDemo(Demo);
        }

        /// <summary>Mở cửa sổ đăng nhập Operator; đăng nhập đúng tài khoản Operator mới vào được bảng điều khiển (NFR 2.2.4).</summary>
        public void OpenOperatorDashboard()
        {
            if (_window is not null)
            {
                _window.Activate();
                return;
            }

            // Phiên đăng nhập riêng cho Operator để không đè phiên của sinh viên.
            _operatorAuth ??= CreateOperatorAuth();
            _operatorAuth.Logout();
            var login = new OperatorLoginWindow(_operatorAuth) { Owner = Application.Current.MainWindow };
            login.LoggedIn += account =>
            {
                var window = new OperatorMainWindow(Demo);
                window.SetCurrentUser(account);
                window.Closed += (_, _) => _window = null;
                _window = window;
                window.Show();
                login.Close();
            };
            login.Show();
        }

        private static CoreServices.AuthService CreateOperatorAuth()
        {
            var auth = new CoreServices.AuthService(DemoApp.Clock);
            global::SmartEMobilityHub.Core.Seed.AccountSeed.Apply(auth);
            return auth;
        }
    }
}
