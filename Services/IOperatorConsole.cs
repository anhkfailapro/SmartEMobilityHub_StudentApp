using SmartEMobilityHub.Core;

namespace SmartEMobilityHub.Services
{
    /// <summary>
    /// Cửa sổ Trung tâm điều hành mở từ Demo panel, trên CHÍNH phiên mạng lưới của app Sinh viên (DC-54).
    /// App Sinh viên chỉ biết interface này, không tham chiếu project Operator: bản thật nằm ở
    /// <c>SmartEMobilityHub.Launcher</c>, chương trình mở cả hai app trong một tiến trình.
    /// Chạy app Sinh viên riêng lẻ thì không có console (Demo panel báo cần chạy Launcher).
    /// </summary>
    public interface IOperatorConsole
    {
        /// <summary>Cửa sổ Operator đang mở.</summary>
        bool IsOpen { get; }

        /// <summary>Mở cửa sổ đăng nhập Operator (hoặc đưa cửa sổ đang mở lên trước) trên phiên mạng lưới này.</summary>
        void Open(NetworkSession session);

        /// <summary>App Sinh viên đã đặt lại demo và tạo phiên mới: cửa sổ Operator đang mở chuyển sang phiên đó.</summary>
        void SessionChanged(NetworkSession session);
    }
}
