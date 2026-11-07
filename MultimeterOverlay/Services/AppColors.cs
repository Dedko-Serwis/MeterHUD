using System.Windows.Media;

namespace MultimeterOverlay.Services;

/// <summary>Single source for user-editable overlay defaults and read-only application theme colors.</summary>
public static class AppColors
{
    // User-editable overlay color defaults (saved custom colors remain in settings.json).
    public const string OverlayBackground = "#181818";
    public const string OverlayText = "#FFFFFF";
    public const string OverlayConnected = "#00B72E";
    public const string OverlayWaiting = "#E0D725";
    public const string OverlayDisconnected = "#585858";
    public const string OverlayRangeText = "#AAAAAA";
    public const string OverlayMinMax = "#0071E1";
    public const string OverlayAc = "#D96C00";
    public const string OverlayDc = "#00C400";
    public const string OverlayHold = "#FF8000";

    public static Color OverlayBackgroundValue => Parse(OverlayBackground);
    public static Color OverlayTextValue => Parse(OverlayText);
    public static Color OverlayConnectedValue => Parse(OverlayConnected);
    public static Color OverlayWaitingValue => Parse(OverlayWaiting);
    public static Color OverlayDisconnectedValue => Parse(OverlayDisconnected);
    public static Color OverlayRangeTextValue => Parse(OverlayRangeText);
    public static Color OverlayMinMaxValue => Parse(OverlayMinMax);
    public static Color OverlayAcValue => Parse(OverlayAc);
    public static Color OverlayDcValue => Parse(OverlayDc);
    public static Color OverlayHoldValue => Parse(OverlayHold);

    // Read-only application UI colors.
    public static SolidColorBrush Ui202020Brush { get; } = CreateBrush("#202020");
    public static SolidColorBrush Ui777777Brush { get; } = CreateBrush("#777777");
    public static SolidColorBrush Ui555555Brush { get; } = CreateBrush("#555555");
    public static SolidColorBrush Ui252525Brush { get; } = CreateBrush("#252525");
    public static SolidColorBrush UiDDDDDDBrush { get; } = CreateBrush("#DDDDDD");
    public static SolidColorBrush Ui444444Brush { get; } = CreateBrush("#444444");
    public static SolidColorBrush Ui333333Brush { get; } = CreateBrush("#333333");
    public static SolidColorBrush UiAAAAAABrush { get; } = CreateBrush("#AAAAAA");
    public static SolidColorBrush Ui27618ABrush { get; } = CreateBrush("#27618A");
    public static SolidColorBrush Ui10304ABrush { get; } = CreateBrush("#10304A");
    public static SolidColorBrush Ui888888Brush { get; } = CreateBrush("#888888");
    public static SolidColorBrush Ui999999Brush { get; } = CreateBrush("#999999");
    public static SolidColorBrush Ui666666Brush { get; } = CreateBrush("#666666");
    public static SolidColorBrush Ui181818Brush { get; } = CreateBrush("#181818");
    public static SolidColorBrush UiWhiteBrush { get; } = CreateBrush("#FFFFFF");
    public static SolidColorBrush UiBlackBrush { get; } = CreateBrush("#000000");
    public static SolidColorBrush UiOrangeRedBrush { get; } = CreateBrush("#FF4500");
    public static SolidColorBrush UiRedBrush { get; } = CreateBrush("#FF0000");
    public static SolidColorBrush UiLimeGreenBrush { get; } = CreateBrush("#32CD32");
    public static SolidColorBrush UiGrayBrush { get; } = CreateBrush("#808080");

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush(Parse(hex));
        brush.Freeze();
        return brush;
    }
}
