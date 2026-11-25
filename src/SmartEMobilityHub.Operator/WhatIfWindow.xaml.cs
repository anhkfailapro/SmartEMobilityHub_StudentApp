using System.IO;
using System.Windows;
using Microsoft.Win32;
using SmartEMobilityHub.Core.Services;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Operator.ViewModels;

namespace SmartEMobilityHub.Operator;

public partial class WhatIfWindow : Window
{
    private readonly WhatIfViewModel _viewModel;

    public WhatIfWindow(WhatIfController controller, NetworkStore store, Func<DateTime> now)
    {
        Themes.OperatorTheme.EnsureLoaded();
        InitializeComponent();
        MainWindow.FitToWorkArea(this);
        _viewModel = new WhatIfViewModel(controller, store, now);
        DataContext = _viewModel;
    }

    private void Run_Click(object sender, RoutedEventArgs e) => _viewModel.RunSimulation();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Cancel_Click(object sender, RoutedEventArgs e) => _viewModel.CancelConfiguration();

    private void ApplyRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string recommendationId })
            _viewModel.ApplyRecommendation(recommendationId);
    }

    private void Rerun_Click(object sender, RoutedEventArgs e) => _viewModel.Rerun();

    private void Export_Click(object sender, RoutedEventArgs e)
    {
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
            if (dialog.ShowDialog(this) != true) return;
            File.WriteAllBytes(dialog.FileName, _viewModel.ExportReport());
            MessageBox.Show(this, "Đã xuất báo cáo PDF.", "What-if",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            MessageBox.Show(this, ex.Message, "Không thể xuất báo cáo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
