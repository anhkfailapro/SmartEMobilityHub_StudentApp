using System.Windows;
using System.Windows.Threading;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator.Themes;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Operator;

public partial class MainWindow : Window
{
    private OperatorDemo _demo = null!;
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
        OperatorTheme.Changed += UpdateThemeButton;
        Closed += (_, _) => OperatorTheme.Changed -= UpdateThemeButton;

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

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        OperatorTheme.Toggle();
        UpdateThemeButton();
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
        _viewModel = new MainViewModel(_demo);
        DataContext = _viewModel;
    }

    private void Run(Func<string?> action)
    {
        var error = action();
        if (error is not null)
            MessageBox.Show(this, error, "Không thực hiện được", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ---------- Header ----------
    private void Scan_Click(object sender, RoutedEventArgs e) => _viewModel.ScanNow();
    private void WhatIf_Click(object sender, RoutedEventArgs e)
    {
        var window = new WhatIfWindow(_demo.WhatIf, _demo.Store, () => _demo.Clock.Now)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    // ---------- UC-OP-03 ----------
    private void Lock_Click(object sender, RoutedEventArgs e) => Run(_viewModel.LockSelected);
    private void Task_Click(object sender, RoutedEventArgs e) => Run(_viewModel.CreateTaskSelected);
    private void Assign_Click(object sender, RoutedEventArgs e) => Run(_viewModel.AssignSelected);
    private void Resolve_Click(object sender, RoutedEventArgs e) => Run(_viewModel.ResolveSelected);
    private void Close_Click(object sender, RoutedEventArgs e) => Run(_viewModel.CloseSelected);
    private void Resume_Click(object sender, RoutedEventArgs e) => Run(_viewModel.ResumeSelectedHub);

    private void Suspend_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedIncident is null) return;

        var hubId = _viewModel.SelectedIncident.HubId;
        var confirm = MessageBox.Show(
            this,
            $"Tạm dừng {hubId}?\nToàn bộ đặt chỗ sắp tới tại Hub này sẽ bị huỷ và người dùng được thông báo.",
            "Xác nhận tạm dừng Hub",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes) Run(_viewModel.SuspendSelectedHub);
    }

    // ---------- Demo panel ----------
    private void BreakPort_Click(object sender, RoutedEventArgs e) => _viewModel.BreakPort();
    private void DisconnectHub_Click(object sender, RoutedEventArgs e) => _viewModel.DisconnectHub();
    private void Stale_Click(object sender, RoutedEventArgs e) => _viewModel.MakeStale();
    private void BreakChannel_Click(object sender, RoutedEventArgs e) => _viewModel.BreakChannel();
    private void RestoreChannel_Click(object sender, RoutedEventArgs e) => _viewModel.RestoreChannel();
    private void Unlock_Click(object sender, RoutedEventArgs e) => _viewModel.RaiseUnlockIncident();
    private void ChargeDemo_Click(object sender, RoutedEventArgs e) => _viewModel.AddChargingRequests();

    // ---------- UC-OP-04: Lịch sạc thông minh ----------
    private void QueueUp_Click(object sender, RoutedEventArgs e) => Run(() => _viewModel.MoveSelectedQueueRow(-1));
    private void QueueDown_Click(object sender, RoutedEventArgs e) => Run(() => _viewModel.MoveSelectedQueueRow(1));
    private void Reschedule_Click(object sender, RoutedEventArgs e) => _viewModel.RescheduleCharging();
    private void ApplyWeights_Click(object sender, RoutedEventArgs e) => Run(_viewModel.ApplyPriorityWeights);
    private void ResetWeights_Click(object sender, RoutedEventArgs e) => _viewModel.ResetPriorityWeights();

    private void Plus1_Click(object sender, RoutedEventArgs e) => _viewModel.Advance(1);
    private void Plus5_Click(object sender, RoutedEventArgs e) => _viewModel.Advance(5);
    private void Plus15_Click(object sender, RoutedEventArgs e) => _viewModel.Advance(15);

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_linked)
        {
            MessageBox.Show(this,
                "Trung tâm điều hành đang dùng chung dữ liệu với app Sinh viên.\nHãy dùng \"Đặt lại toàn bộ demo\" ở Demo panel của app Sinh viên để đặt lại cả hai bên.",
                "Đặt lại demo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "Đặt lại toàn bộ dữ liệu demo? Mọi thay đổi sẽ mất.",
            "Đặt lại demo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes) ResetDemo();
    }

    private void SuggestDispatch_Click(object sender, RoutedEventArgs e)
    {
        var err = _viewModel.SuggestAndCreateDispatch();
        if (err is not null)
            MessageBox.Show(err, "Điều phối", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CreateDispatch_Click(object sender, RoutedEventArgs e)
    {
        var err = _viewModel.CreateDispatchOrder();
        if (err is not null)
            MessageBox.Show(err, "Điều phối", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ExecuteDispatch_Click(object sender, RoutedEventArgs e)
    {
        var err = _viewModel.ExecuteSelectedDispatch();
        if (err is not null)
            MessageBox.Show(err, "Điều phối", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CompleteDispatch_Click(object sender, RoutedEventArgs e)
    {
        var err = _viewModel.CompleteSelectedDispatch();
        if (err is not null)
            MessageBox.Show(err, "Điều phối", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
