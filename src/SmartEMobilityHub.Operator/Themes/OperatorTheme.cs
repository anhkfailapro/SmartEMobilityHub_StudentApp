using System.Windows;

namespace SmartEMobilityHub.Operator.Themes;

/// <summary>
/// Chế độ giao diện Sáng/Tối của Trung tâm điều hành (UC-S-12 / NFR 2.2.5 áp dụng cho Operator).
/// Bảng màu (Light.xaml / Dark.xaml) và Styles.xaml được nạp vào tài nguyên của Application đang chạy,
/// nên dùng được cả khi cửa sổ Operator được mở từ app Sinh viên (cùng tiến trình).
/// Mọi màu trong XAML dùng DynamicResource, nên đổi bảng màu là cửa sổ đổi ngay.
/// </summary>
public static class OperatorTheme
{
    private const string BaseUri = "pack://application:,,,/SmartEMobilityHub.Operator;component/Themes/";

    private static ResourceDictionary? _palette;
    private static ResourceDictionary? _styles;

    public static bool IsDark { get; private set; }

    public static event Action? Changed;

    /// <summary>Gọi trước khi tạo cửa sổ Operator đầu tiên. Gọi nhiều lần không sao.</summary>
    public static void EnsureLoaded()
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;
        if (_palette is not null && merged.Contains(_palette)) return;

        _palette = Load(IsDark ? "Dark.xaml" : "Light.xaml");
        _styles = Load("Styles.xaml");
        merged.Add(_palette);
        merged.Add(_styles);
    }

    public static void SetDark(bool dark)
    {
        EnsureLoaded();
        if (dark == IsDark || _palette is null) return;

        IsDark = dark;
        var merged = Application.Current.Resources.MergedDictionaries;
        var next = Load(dark ? "Dark.xaml" : "Light.xaml");
        var index = merged.IndexOf(_palette);
        if (index >= 0) merged[index] = next;
        else merged.Insert(0, next);
        _palette = next;
        Changed?.Invoke();
    }

    public static void Toggle() => SetDark(!IsDark);

    private static ResourceDictionary Load(string file) => new() { Source = new Uri(BaseUri + file, UriKind.Absolute) };
}
