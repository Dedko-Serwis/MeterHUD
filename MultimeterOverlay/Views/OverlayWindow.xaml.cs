
using MultimeterOverlay.Services;
using MultimeterOverlay.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MultimeterOverlay.Views;

public partial class OverlayWindow : Window
{
    private readonly AppSettings _settings;

    public OverlayWindow(
        MainViewModel viewModel,
        AppSettings settings)
    {
        InitializeComponent();

        DataContext = viewModel;
        _settings = settings;
        ApplyAppearance();
        ApplySizeLock();
        ApplyTopmost();

        Loaded += OverlayWindow_Loaded;
        Closing += OverlayWindow_Closing;
    }

    private void OverlayWindow_Loaded(
        object sender, RoutedEventArgs e)
    {
        RestorePosition();
    }

    private void OverlayWindow_Closing(
        object? sender,
        System.ComponentModel.CancelEventArgs e)
    {
        SavePosition();
    }

    public void ApplyAppearance()
    {
        Resources["OverlayBackgroundBrush"] = ParseColor(_settings.OverlayBackgroundColor, AppColors.OverlayBackground);
        Resources["OverlayTextBrush"] = ParseColor(_settings.OverlayTextColor, AppColors.OverlayText);
        Resources["OverlayConnectedBrush"] = ParseColor(_settings.OverlayConnectedColor, AppColors.OverlayConnected);
        Resources["OverlayWaitingBrush"] = ParseColor(_settings.OverlayWaitingColor, AppColors.OverlayWaiting);
        Resources["OverlayDisconnectedBrush"] = ParseColor(_settings.OverlayDisconnectedColor, AppColors.OverlayDisconnected);
        Resources["OverlayRangeTextBrush"] = ParseColor(_settings.OverlayRangeTextColor, AppColors.OverlayRangeText);
        Resources["OverlayMinMaxBrush"] = ParseColor(_settings.OverlayMinMaxColor, AppColors.OverlayMinMax);
        Resources["OverlayAcBrush"] = ParseColor(_settings.OverlayAcColor, AppColors.OverlayAc);
        Resources["OverlayDcBrush"] = ParseColor(_settings.OverlayDcColor, AppColors.OverlayDc);
        Resources["OverlayHoldBrush"] = ParseColor(_settings.OverlayHoldColor, AppColors.OverlayHold);
    }

    public void ApplySizeLock()
    {
        ResizeMode = _settings.OverlaySizeLocked
            ? ResizeMode.NoResize
            : ResizeMode.CanResizeWithGrip;
    }

    public void ApplyTopmost()
    {
        Topmost = _settings.OverlayTopmost;
    }

    private static SolidColorBrush ParseColor(string? value, string fallback)
    {
        if (value is null || !System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$"))
        {
            value = fallback;
        }

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    }

    public void SavePosition()
    {
        Rect bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        _settings.OverlayLeft = bounds.Left;
        _settings.OverlayTop = bounds.Top;
        _settings.OverlayWidth = bounds.Width;
        _settings.OverlayHeight = bounds.Height;
    }

    public void ResetPosition()
    {
        WindowState = WindowState.Normal;
        Width = 500;
        Height = 180;
        Left = 0;
        Top = 0;
    }

    private void RestorePosition()
    {
        double width = _settings.OverlayWidth;
        double height = _settings.OverlayHeight;

        if (!double.IsFinite(width) || width < MinWidth ||
            !double.IsFinite(height) || height < MinHeight ||
            width > SystemParameters.VirtualScreenWidth ||
            height > SystemParameters.VirtualScreenHeight)
        {
            ResetPosition();
            return;
        }

        Width = width;
        Height = height;

        double left = _settings.OverlayLeft ?? double.NaN;
        double top = _settings.OverlayTop ?? double.NaN;

        if (!double.IsFinite(left) || !double.IsFinite(top))
        {
            ResetPosition();
            return;
        }

        // Kontrolujemy, czy przynajmniej część okna
        // pozostanie widoczna na ekranie.
        Rect savedBounds = new(left, top, width, height);

        Rect virtualScreen = new(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        if (!savedBounds.IntersectsWith(virtualScreen))
        {
            ResetPosition();
            return;
        }

        Left = left;
        Top = top;
    }

    protected override void OnMouseLeftButtonDown(
        MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState == MouseButtonState.Pressed &&
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            DragMove();
        }
    }
}
