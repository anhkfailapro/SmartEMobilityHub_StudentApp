using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartEMobilityHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Height = Math.Min(Height, SystemParameters.WorkArea.Height - 20);
            // Chế độ chụp màn hình cho phần Working demonstration: dotnet run -- --capture demo_screens\student
            var args = Environment.GetCommandLineArgs();
            var ci = Array.IndexOf(args, "--capture");
            // Chế độ chụp tự đăng nhập tài khoản demo để kịch bản đi thẳng vào Trang chủ.
            DemoApp.Start(PhoneHost, DemoPanelHost, requireLogin: ci < 0);
            if (ci >= 0)
            {
                var dir = ci + 1 < args.Length ? args[ci + 1] : "demo_screens/student";
                Loaded += async (_, _) => await Capture.StudentCapture.RunAsync(PhoneHost, dir);
            }
        }
    }
}
