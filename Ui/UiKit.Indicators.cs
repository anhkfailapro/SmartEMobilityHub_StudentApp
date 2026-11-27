using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SmartEMobilityHub.Data;
using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Ui
{
    /// <summary>Bộ dựng giao diện của app Sinh viên — phần Chỉ báo: Progress, BatteryBar, mã QR giả lập.</summary>
    public static partial class UiKit
    {
        public static Grid Progress(double fraction, Brush fill, Brush track, double height = 8)
        {
            fraction = Math.Clamp(fraction, 0, 1);
            var g = new Grid { Height = height };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(fraction, 0.0001), GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(1 - fraction, 0.0001), GridUnitType.Star) });
            var bg = new Border { Background = track, CornerRadius = new CornerRadius(height / 2) };
            Grid.SetColumnSpan(bg, 2);
            var fg = new Border { Background = fill, CornerRadius = new CornerRadius(height / 2) };
            g.Children.Add(bg);
            g.Children.Add(fg);
            return g;
        }

        public static Grid BatteryBar(int percent)
        {
            var color = percent > 50 ? Theme.Success : percent >= DemoConfig.NeedsChargeBelow ? Theme.Warning : Theme.Danger;
            return Progress(percent / 100.0, color, Theme.SurfaceAlt);
        }

        public static Canvas Qr(string seedText, double size = 132)
        {
            var canvas = new Canvas { Width = size, Height = size, Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
            var cell = size / 11;
            var seed = seedText.Aggregate(7, (a, ch) => unchecked(a * 31 + ch)) & 0x7fffffff;
            var rnd = new Random(seed);
            for (var y = 0; y < 11; y++)
            {
                for (var x = 0; x < 11; x++)
                {
                    var finder = (x < 3 && y < 3) || (x > 7 && y < 3) || (x < 3 && y > 7);
                    if (!finder && rnd.NextDouble() <= 0.55) continue;
                    var r = new System.Windows.Shapes.Rectangle { Width = cell, Height = cell, Fill = Brushes.Black };
                    Canvas.SetLeft(r, x * cell);
                    Canvas.SetTop(r, y * cell);
                    canvas.Children.Add(r);
                }
            }
            return canvas;
        }
    }
}
