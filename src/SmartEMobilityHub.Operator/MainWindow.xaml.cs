using System.Windows;
using System.Windows.Threading;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Operator;

public partial class MainWindow : Window
{
    private OperatorDemo _demo = null!;
    private MainViewModel _viewModel = null!;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public MainWindow()
    {
        InitializeComponent();
        ResetDemo();

        _timer.Tick += (_, _) => _viewModel.Tick();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private void ResetDemo()
    {
        _demo = new OperatorDemo();
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

    // ---------- UC-OP-03 ----------
    private void Lock_Click(object sender, RoutedEventArgs e) => Run(_viewModel.LockSelected);
    private void Task_Click(object sender, RoutedEventArgs e) => Run(_viewModel.CreateTaskSelected);
    private void Assign_Click(object sender, RoutedEventArgs e) => Run(_viewModel.AssignSelected);
    private void Resolve_Click(object sender, RoutedEventArgs e) => Run(_viewModel.ResolveSelected);
    private void Close_Click(object sender, RoutedEventArgs e) => Run(_viewModel.CloseSelected);

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

    private void Plus1_Click(object sender, RoutedEventArgs e) => _viewModel.Advance(1);
    private void Plus5_Click(object sender, RoutedEventArgs e) => _viewModel.Advance(5);
    private void Plus15_Click(object sender, RoutedEventArgs e) => _viewModel.Advance(15);

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
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
