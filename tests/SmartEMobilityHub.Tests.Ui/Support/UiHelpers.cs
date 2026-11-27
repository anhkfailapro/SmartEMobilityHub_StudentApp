using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartEMobilityHub.Tests.Ui.Support;

public static class UiHelpers
{
    /// <summary>Cho Dispatcher chạy hết việc đang chờ (binding, layout, Loaded) trước khi kiểm tra.</summary>
    public static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    /// <summary>Phần tử con đầu tiên kiểu <typeparamref name="T"/> trong cây hiển thị.</summary>
    public static T? Find<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var nested = Find<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }
}
