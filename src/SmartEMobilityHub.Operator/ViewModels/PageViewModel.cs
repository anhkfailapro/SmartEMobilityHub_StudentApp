using SmartEMobilityHub.Core;
using SmartEMobilityHub.Core.Store;
using SmartEMobilityHub.Operator.Infrastructure;

namespace SmartEMobilityHub.Operator.ViewModels;

/// <summary>ViewModel của một trang trong thanh bên. Trang tự đọc dữ liệu từ phiên mạng lưới và tự làm mới.</summary>
public abstract class PageViewModel : ObservableObject
{
    protected PageViewModel(DashboardContext shell)
    {
        Shell = shell;
    }

    /// <summary>Trạng thái dùng chung (XAML bind <c>Shell.AutoScan</c>, <c>Shell.SpeedUp</c>…).</summary>
    public DashboardContext Shell { get; }

    protected NetworkSession Session => Shell.Session;
    protected NetworkStore Store => Session.Store;
    protected DateTime Now => Session.Clock.Now;

    /// <summary>Đọc lại dữ liệu của trang từ mạng lưới.</summary>
    public abstract void Refresh();

    protected void Note(string message) => Shell.Note(message);

    /// <summary>Sau một thao tác đổi dữ liệu: vẽ lại mọi trang (KPI, huy hiệu ở trang khác cũng đổi theo).</summary>
    protected void RefreshAll() => Shell.RequestRefresh();

    protected void Run(Func<string?> action, string title = DashboardContext.ActionFailedTitle) => Shell.Run(action, title);
}
