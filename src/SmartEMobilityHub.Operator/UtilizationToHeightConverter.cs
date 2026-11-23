using System.Globalization;
using System.Windows.Data;

namespace SmartEMobilityHub.Operator;

/// <summary>Maps a utilization ratio to a bottom-anchored chart bar height.</summary>
public sealed class UtilizationToHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 ||
            values[0] is not double utilization ||
            values[1] is not double scale ||
            !double.TryParse(parameter?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out var plotHeight) || scale <= 0)
            return 0d;

        return plotHeight * Math.Clamp(utilization / scale, 0, 1);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
