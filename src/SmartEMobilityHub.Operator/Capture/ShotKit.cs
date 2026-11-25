using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SmartEMobilityHub.Capture
{
    /// <summary>
    /// Tiện ích chụp màn hình tự động cho phần "Working demonstration (sequence of screens)".
    /// Chỉ chạy khi app được mở với tham số --capture; không ảnh hưởng chế độ demo bình thường.
    /// </summary>
    internal sealed class ShotKit
    {
        private readonly string _dir;
        private readonly List<ShotInfo> _shots = new();
        private readonly List<string> _log = new();
        private int _n;

        public string Section { get; set; } = "";
        public int Count => _n;
        public string Dir => _dir;

        public ShotKit(string dir)
        {
            _dir = Path.GetFullPath(dir);
            Directory.CreateDirectory(_dir);
        }

        public sealed record ShotInfo(int Index, string File, string Section, string Title, string Caption, int Width, int Height);

        public void Log(string message) => _log.Add($"[shot {_n}] {message}");

        public void LogResult(string step, string? error)
        {
            if (error != null) Log($"{step}: {error}");
        }

        public static async Task Settle(int ms = 250)
        {
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await Task.Delay(ms);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        }

        public static List<FrameworkElement> Flatten(DependencyObject root)
        {
            (root as UIElement)?.UpdateLayout();
            var list = new List<FrameworkElement>();
            void Rec(DependencyObject d)
            {
                if (d is FrameworkElement fe) list.Add(fe);
                var count = VisualTreeHelper.GetChildrenCount(d);
                for (var i = 0; i < count; i++) Rec(VisualTreeHelper.GetChild(d, i));
            }
            Rec(root);
            return list;
        }

        private static FrameworkElement? ClickableAncestor(DependencyObject start, DependencyObject root)
        {
            DependencyObject? d = start;
            while (d != null)
            {
                if (d is FrameworkElement fe && fe.Cursor == Cursors.Hand) return fe;
                if (ReferenceEquals(d, root)) break;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        private static bool Matches(TextBlock t, string text, bool prefix)
        {
            var s = t.Text ?? "";
            return prefix ? s.StartsWith(text, StringComparison.Ordinal) : s == text;
        }

        private static void Raise(UIElement el)
        {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                Source = el
            };
            el.RaiseEvent(args);
        }

        /// <summary>Bấm vào phần tử có chữ <paramref name="text"/> (giống người dùng bấm).</summary>
        public bool Click(DependencyObject root, string text, bool prefix = false, int nth = 0, bool last = false)
        {
            var targets = Flatten(root)
                .OfType<TextBlock>()
                .Where(t => t.IsVisible && Matches(t, text, prefix))
                .Select(t => ClickableAncestor(t, root))
                .Where(c => c != null)
                .Distinct()
                .ToList();
            if (targets.Count == 0)
            {
                Log($"Không tìm thấy nút/chữ: \"{text}\"");
                return false;
            }
            var target = last ? targets[^1] : targets[Math.Min(nth, targets.Count - 1)];
            Raise(target!);
            return true;
        }

        /// <summary>Bấm phần tử bấm được đầu tiên nằm sau dòng chữ mốc.</summary>
        public bool ClickAfter(DependencyObject root, string anchor, bool prefix = true)
        {
            var all = Flatten(root);
            var idx = all.FindIndex(f => f is TextBlock t && t.IsVisible && Matches(t, anchor, prefix));
            if (idx < 0)
            {
                Log($"Không tìm thấy mốc: \"{anchor}\"");
                return false;
            }
            var target = all.Skip(idx + 1).FirstOrDefault(f => f.IsVisible && f.Cursor == Cursors.Hand);
            if (target == null)
            {
                Log($"Không có phần tử bấm được sau mốc: \"{anchor}\"");
                return false;
            }
            Raise(target);
            return true;
        }

        public bool Type(DependencyObject root, int index, string text)
        {
            var boxes = Flatten(root).OfType<TextBox>().Where(t => t.IsVisible).ToList();
            if (index >= boxes.Count)
            {
                Log($"Không có ô nhập thứ {index}");
                return false;
            }
            boxes[index].Text = text;
            return true;
        }

        public static (int W, int H) SavePng(FrameworkElement el, string path, double scale)
        {
            el.UpdateLayout();
            var w = el.ActualWidth;
            var h = el.ActualHeight;
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var rect = new Rect(0, 0, w, h);
                dc.DrawRectangle(Brushes.White, null, rect);
                var brush = new VisualBrush(el)
                {
                    Stretch = Stretch.Fill,
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = rect,
                    ViewportUnits = BrushMappingMode.Absolute,
                    Viewport = rect
                };
                dc.DrawRectangle(brush, null, rect);
            }
            var pw = (int)Math.Ceiling(w * scale);
            var ph = (int)Math.Ceiling(h * scale);
            var rtb = new RenderTargetBitmap(pw, ph, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using var fs = File.Create(path);
            enc.Save(fs);
            return (pw, ph);
        }

        public void Save(FrameworkElement el, string title, string caption, double scale)
        {
            _n++;
            var file = $"{_n:00}.png";
            try
            {
                var (w, h) = SavePng(el, Path.Combine(_dir, file), scale);
                _shots.Add(new ShotInfo(_n, file, Section, title, caption, w, h));
            }
            catch (Exception ex)
            {
                Log($"Lỗi khi chụp \"{title}\": {ex.Message}");
            }
        }

        public void Finish()
        {
            var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(Path.Combine(_dir, "manifest.json"), JsonSerializer.Serialize(_shots, opts));
            File.WriteAllLines(Path.Combine(_dir, "capture_log.txt"), _log.Count == 0 ? new[] { "OK – không có lỗi." } : _log);
        }
    }
}
