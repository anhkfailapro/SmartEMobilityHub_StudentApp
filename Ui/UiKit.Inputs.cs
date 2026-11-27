using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    /// <summary>Bộ dựng giao diện của app Sinh viên — phần Ô nhập: InputBox, PasswordInput, Toggle.</summary>
    public static partial class UiKit
    {
        public static Border InputBox(string placeholder, string text, Action<string> onChange, bool multiline = false, Action? onEnter = null)
        {
            var tb = new TextBox
            {
                Text = text,
                FontFamily = Font,
                FontSize = 14,
                Background = Brushes.Transparent,
                Foreground = Theme.Text,
                CaretBrush = Theme.Text,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center
            };
            if (multiline)
            {
                tb.AcceptsReturn = true;
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Height = 88;
            }
            var hint = Text(placeholder, TextKind.Body, Theme.Text2);
            hint.IsHitTestVisible = false;
            hint.VerticalAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center;
            hint.Margin = new Thickness(2, multiline ? 1 : 0, 0, 0);
            hint.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            tb.TextChanged += (_, _) =>
            {
                hint.Visibility = string.IsNullOrEmpty(tb.Text) ? Visibility.Visible : Visibility.Collapsed;
                onChange(tb.Text);
            };
            if (onEnter != null && !multiline)
                tb.KeyDown += (_, e) =>
                {
                    if (e.Key != System.Windows.Input.Key.Enter) return;
                    e.Handled = true;
                    onEnter();
                };
            var grid = new Grid();
            grid.Children.Add(hint);
            grid.Children.Add(tb);
            return FocusFrame(grid, tb);
        }

        /// <summary>Ô nhập mật khẩu (ẩn ký tự), cùng kiểu dáng với <see cref="InputBox"/>.</summary>
        public static Border PasswordInput(string placeholder, string password, Action<string> onChange, Action? onEnter = null)
        {
            var pb = new PasswordBox
            {
                Password = password,
                FontFamily = Font,
                FontSize = 14,
                Background = Brushes.Transparent,
                Foreground = Theme.Text,
                CaretBrush = Theme.Text,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var hint = Text(placeholder, TextKind.Body, Theme.Text2);
            hint.IsHitTestVisible = false;
            hint.VerticalAlignment = VerticalAlignment.Center;
            hint.Margin = new Thickness(2, 0, 0, 0);
            hint.Visibility = string.IsNullOrEmpty(password) ? Visibility.Visible : Visibility.Collapsed;
            pb.PasswordChanged += (_, _) =>
            {
                hint.Visibility = string.IsNullOrEmpty(pb.Password) ? Visibility.Visible : Visibility.Collapsed;
                onChange(pb.Password);
            };
            if (onEnter != null)
                pb.KeyDown += (_, e) =>
                {
                    if (e.Key != System.Windows.Input.Key.Enter) return;
                    e.Handled = true;
                    onEnter();
                };
            var grid = new Grid();
            grid.Children.Add(hint);
            grid.Children.Add(pb);
            return FocusFrame(grid, pb);
        }

        /// <summary>Khung ô nhập: bo tròn, viền chuyển màu chính khi đang nhập.</summary>
        private static Border FocusFrame(UIElement content, Control input)
        {
            var frame = new Border
            {
                Background = Theme.Surface,
                BorderBrush = Theme.BorderStrong,
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 11, 14, 11),
                Child = content
            };
            input.GotKeyboardFocus += (_, _) => frame.BorderBrush = Theme.Primary;
            input.LostKeyboardFocus += (_, _) => frame.BorderBrush = Theme.BorderStrong;
            return frame;
        }

        public static Grid Toggle(string label, bool value, Action<bool> onChange)
        {
            var knob = new System.Windows.Shapes.Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Margin = new Thickness(3),
                HorizontalAlignment = value ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            var track = new Border
            {
                Width = 44,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = value ? Theme.PrimaryGradient : Theme.BorderStrong,
                Child = knob
            };
            OnClick(track, () => onChange(!value));
            return Split(Text(label, TextKind.Subtitle), track);
        }
    }
}
