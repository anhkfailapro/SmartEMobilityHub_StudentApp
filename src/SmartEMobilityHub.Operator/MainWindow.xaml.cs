using System.Windows;
using System.Windows.Threading;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Infrastructure;
using SmartEMobilityHub.Operator.Themes;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Operator;

public partial class MainWindow : Window
{
    private OperatorDemo _demo = null!;

    /// <summary>Mạng lưới đang gắn với cửa sổ (dùng cho kịch bản chụp màn hình).</summary>
    internal OperatorDemo Demo => _demo;
    private MainViewModel _viewModel = null!;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>true khi cửa sổ dùng chung mạng lưới với app Sinh viên (mở từ Demo panel của app Sinh viên).</summary>
    private readonly bool _linked;

    public MainWindow() : this(null)
    {
    }

    /// <param name="sharedDemo">
    /// Mạng lưới dùng chung với app Sinh viên. null = Trung tâm điều hành chạy độc lập với dữ liệu riêng.
    /// </param>
    public MainWindow(OperatorDemo? sharedDemo)
    {
        OperatorTheme.EnsureLoaded();
        InitializeComponent();
        UpdateThemeButton();
        FitToWorkArea(this);
        OperatorTheme.Changed += OnThemeChanged;
        Closed += (_, _) => OperatorTheme.Changed -= OnThemeChanged;

        _linked = sharedDemo is not null;
        if (sharedDemo is null) ResetDemo();
        else AttachDemo(sharedDemo);
        if (_linked) Title += " · Liên thông với app Sinh viên";

        _timer.Tick += (_, _) => _viewModel.Tick();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();

        // Chế độ chụp màn hình cho phần Working demonstration.
        var args = Environment.GetCommandLineArgs();
        var ci = Array.IndexOf(args, "--capture");
        if (ci >= 0)
        {
            var dir = ci + 1 < args.Length ? args[ci + 1] : "demo_screens/operator";
            Loaded += async (_, _) => await global::SmartEMobilityHub.Capture.OperatorCapture.RunAsync(this, dir);
        }
    }

    /// <summary>
    /// Cửa sổ thiết kế cho màn hình 1520×940; trên laptop (vùng làm việc thấp hơn) cửa sổ bị tràn khỏi màn hình
    /// và mất header. Thu nhỏ vừa vùng làm việc, không nhỏ hơn kích thước tối thiểu.
    /// </summary>
    public static void FitToWorkArea(Window window)
    {
        var area = SystemParameters.WorkArea;
        window.MinHeight = Math.Min(window.MinHeight, area.Height);
        window.MinWidth = Math.Min(window.MinWidth, area.Width);
        window.Height = Math.Min(window.Height, area.Height - 10);
        window.Width = Math.Min(window.Width, area.Width - 10);
    }

    /// <summary>Hiện người vận hành đang đăng nhập; vai trò và ca trực lấy từ seed Operator nếu có.</summary>
    public void SetCurrentUser(UserAccount account)
    {
        var op = _demo.Store.Operators.FirstOrDefault(o => o.OperatorId == account.LinkedId);
        OperatorNameText.Text = op?.Name ?? account.DisplayName;
        OperatorRoleText.Text = op is null ? account.LinkedId : $"{op.Role} · {op.ShiftLabel}";
    }

    private void Theme_Click(object sender, RoutedEventArgs e) => OperatorTheme.Toggle();

    /// <summary>Đổi Sáng/Tối: cập nhật nút và dựng lại các dòng có màu nhãn theo chế độ (nhãn trạng thái, thẻ Hub…).</summary>
    private void OnThemeChanged()
    {
        UpdateThemeButton();
        _viewModel?.Refresh();
    }

    private void UpdateThemeButton() =>
        ThemeButton.Content = OperatorTheme.IsDark ? "☀ Chế độ sáng" : "☾ Chế độ tối";

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            this,
            "Đăng xuất khỏi Trung tâm điều hành?",
            "Đăng xuất",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        // Khi mở từ app Sinh viên (chế độ liên thông), Application hiện tại không phải App của Operator:
        // đăng xuất chỉ đóng cửa sổ điều hành.
        if (Application.Current is App app) app.Logout(this);
        else Close();
    }

    private void ResetDemo() => AttachDemo(new OperatorDemo());

    /// <summary>Gắn cửa sổ vào một mạng lưới (dùng khi app Sinh viên đặt lại demo và tạo mạng lưới mới).</summary>
    public void AttachDemo(OperatorDemo demo)
    {
        _demo = demo;
        _viewModel = new MainViewModel(_demo)
        {
            Dialogs = new WindowDialogService(this),
            IsLinked = _linked,
            ResetRequested = ResetDemo
        };
        DataContext = _viewModel;
    }

    // ---------- Việc của cửa sổ (các nút trong trang dùng lệnh của MainViewModel — DC-52) ----------

    private void WhatIf_Click(object sender, RoutedEventArgs e)
    {
        var window = new WhatIfWindow(_demo.WhatIf, _demo.Store, () => _demo.Clock.Now)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
