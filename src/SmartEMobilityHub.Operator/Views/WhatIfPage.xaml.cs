using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Operator.Views;

/// <summary>
/// Trang "Mô phỏng và phân tích kịch bản" (What-if), nhúng ngay trong MainWindow thay cho cửa sổ riêng.
/// Nút "← Quay lại" phát <see cref="CloseRequested"/> để MainWindow trả về trang đang xem trước đó.
/// </summary>
public partial class WhatIfPage : UserControl
{
    private readonly WhatIfViewModel _viewModel;

    public WhatIfPage(WhatIfController controller, NetworkStore store, Func<DateTime> now)
    {
        Themes.OperatorTheme.EnsureLoaded();
        InitializeComponent();
        _viewModel = new WhatIfViewModel(controller, store, now);
        DataContext = _viewModel;
    }

    /// <summary>Người dùng bấm "← Quay lại": MainWindow ẩn trang What-if.</summary>
    public event EventHandler? CloseRequested;

    public WhatIfViewModel ViewModel => _viewModel;

    private Window? Owner => Window.GetWindow(this);

    private void Run_Click(object sender, RoutedEventArgs e) => _viewModel.RunSimulation();

    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void Cancel_Click(object sender, RoutedEventArgs e) => _viewModel.CancelConfiguration();

    private void ApplyRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string recommendationId })
            _viewModel.ApplyRecommendation(recommendationId);
    }

    private void Rerun_Click(object sender, RoutedEventArgs e) => _viewModel.Rerun();

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var owner = Owner;
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Xuất báo cáo mô phỏng",
                FileName = $"WhatIf_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true
            };
            if (dialog.ShowDialog(owner) != true) return;
            File.WriteAllBytes(dialog.FileName, _viewModel.ExportReport());
            Show(owner, "Đã xuất báo cáo PDF.", "What-if", MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            Show(owner, ex.Message, "Không thể xuất báo cáo", MessageBoxImage.Warning);
        }
    }

    private static void Show(Window? owner, string text, string caption, MessageBoxImage icon)
    {
        if (owner is null) MessageBox.Show(text, caption, MessageBoxButton.OK, icon);
        else MessageBox.Show(owner, text, caption, MessageBoxButton.OK, icon);
    }
}
