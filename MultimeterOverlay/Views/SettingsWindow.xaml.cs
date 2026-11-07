
using MultimeterOverlay.Services;
using MultimeterOverlay.ViewModels;
using System.Windows;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;

namespace MultimeterOverlay.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _viewModel = new SettingsViewModel(settings);

        DataContext = _viewModel;
        BackgroundColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        TextColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        ConnectedColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        WaitingColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        DisconnectedColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        RangeTextColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        MinMaxColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        AcColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        DcColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        HoldColorTextBox.TextChanged += (_, _) => RefreshColorPreviews();
        RefreshColorPreviews();
    }

    private void Save_Click(
        object sender, RoutedEventArgs e)
    {
        if (!TryNormalizeColor(_viewModel.OverlayBackgroundColor, out string background) ||
            !TryNormalizeColor(_viewModel.OverlayTextColor, out string foreground) ||
            !TryNormalizeColor(_viewModel.OverlayConnectedColor, out string connected) ||
            !TryNormalizeColor(_viewModel.OverlayWaitingColor, out string waiting) ||
            !TryNormalizeColor(_viewModel.OverlayDisconnectedColor, out string disconnected) ||
            !TryNormalizeColor(_viewModel.OverlayRangeTextColor, out string rangeText)) ||
            !TryNormalizeColor(_viewModel.OverlayMinMaxColor, out string minMax)) ||
            !TryNormalizeColor(_viewModel.OverlayAcColor, out string ac)) ||
            !TryNormalizeColor(_viewModel.OverlayDcColor, out string dc)) ||
            !TryNormalizeColor(_viewModel.OverlayHoldColor, out string hold))
        {
            MessageBox.Show("Podaj kolory w formacie #RRGGBB.", "MultimeterOverlay",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string previousLanguage = _settings.Language;
        string previousBackground = _settings.OverlayBackgroundColor;
        string previousForeground = _settings.OverlayTextColor;
        string previousConnected = _settings.OverlayConnectedColor;
        string previousWaiting = _settings.OverlayWaitingColor;
        string previousDisconnected = _settings.OverlayDisconnectedColor;
        string previousRangeText = _settings.OverlayRangeTextColor;
        string previousMinMax = _settings.OverlayMinMaxColor;
        string previousAc = _settings.OverlayAcColor;
        string previousDc = _settings.OverlayDcColor;
        string previousHold = _settings.OverlayHoldColor;

        try
        {

            _settings.Language = _viewModel.Language;
            _settings.OverlayBackgroundColor = background;
            _settings.OverlayTextColor = foreground;
            _settings.OverlayConnectedColor = connected;
            _settings.OverlayWaitingColor = waiting;
            _settings.OverlayDisconnectedColor = disconnected;
            _settings.OverlayRangeTextColor = rangeText;
            _settings.OverlayMinMaxColor = minMax;
            _settings.OverlayAcColor = ac;
            _settings.OverlayDcColor = dc;
            _settings.OverlayHoldColor = hold;
            _settings.Save();

            LocalizationService.Instance.SetLanguage(_settings.Language);

            DialogResult = true;

        }
        catch (Exception ex)
        {
            _settings.Language = previousLanguage;
            _settings.OverlayBackgroundColor = previousBackground;
            _settings.OverlayTextColor = previousForeground;
            _settings.OverlayConnectedColor = previousConnected;
            _settings.OverlayWaitingColor = previousWaiting;
            _settings.OverlayDisconnectedColor = previousDisconnected;
            _settings.OverlayRangeTextColor = previousRangeText;
            _settings.OverlayMinMaxColor = previousMinMax;
            _settings.OverlayAcColor = previousAc;
            _settings.OverlayDcColor = previousDc;
            _settings.OverlayHoldColor = previousHold;

            MessageBox.Show(
                string.Format(LocalizationService.Instance["ErrorSettingsSave"], ex.Message),
                "MultimeterOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool TryNormalizeColor(string? value, out string normalized)
    {
        normalized = "";
        if (value is null || value.Length != 7 || value[0] != '#' ||
            !System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$"))
            return false;

        normalized = value.ToUpperInvariant();
        return true;
    }

    private void RefreshColorPreviews()
    {
        if (TryNormalizeColor(_viewModel.OverlayBackgroundColor, out string bg))
            BackgroundColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
        if (TryNormalizeColor(_viewModel.OverlayTextColor, out string fg))
            TextColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg));
        if (TryNormalizeColor(_viewModel.OverlayConnectedColor, out string connected))
            ConnectedColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(connected));
        if (TryNormalizeColor(_viewModel.OverlayWaitingColor, out string waiting))
            WaitingColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(waiting));
        if (TryNormalizeColor(_viewModel.OverlayDisconnectedColor, out string disconnected))
            DisconnectedColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(disconnected));
        if (TryNormalizeColor(_viewModel.OverlayRangeTextColor, out string rangeText))
            RangeTextColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(rangeText));
        if (TryNormalizeColor(_viewModel.OverlayMinMaxColor, out string minMax))
            MinMaxColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(minMax));
        if (TryNormalizeColor(_viewModel.OverlayAcColor, out string ac))
            AcColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ac));
        if (TryNormalizeColor(_viewModel.OverlayDcColor, out string dc))
            DcColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dc));
        if (TryNormalizeColor(_viewModel.OverlayHoldColor, out string hold))
            HoldColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hold));
    }

    private void PickColor(string current, Action<string> setColor)
    {
        using var dialog = new Forms.ColorDialog { FullOpen = true, AnyColor = true };
        if (TryNormalizeColor(current, out string hex))
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            dialog.Color = DrawingColor.FromArgb(color.R, color.G, color.B);
        }
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
            setColor($"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}");
    }

    private void BackgroundColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayBackgroundColor, value => _viewModel.OverlayBackgroundColor = value);

    private void TextColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayTextColor, value => _viewModel.OverlayTextColor = value);

    private void ConnectedColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayConnectedColor, value => _viewModel.OverlayConnectedColor = value);

    private void WaitingColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayWaitingColor, value => _viewModel.OverlayWaitingColor = value);

    private void DisconnectedColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayDisconnectedColor, value => _viewModel.OverlayDisconnectedColor = value);

    private void RangeTextColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayRangeTextColor, value => _viewModel.OverlayRangeTextColor = value);

    private void MinMaxColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayMinMaxColor, value => _viewModel.OverlayMinMaxColor = value);

    private void AcColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayAcColor, value => _viewModel.OverlayAcColor = value);

    private void DcColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayDcColor, value => _viewModel.OverlayDcColor = value);

    private void HoldColor_Click(object sender, RoutedEventArgs e) =>
        PickColor(_viewModel.OverlayHoldColor, value => _viewModel.OverlayHoldColor = value);

    private void DefaultColors_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OverlayBackgroundColor = "#181818";
        _viewModel.OverlayTextColor = "#FFFFFF";
        _viewModel.OverlayConnectedColor = "#39C875";
        _viewModel.OverlayWaitingColor = "#F4D35E";
        _viewModel.OverlayDisconnectedColor = "#777777";
        _viewModel.OverlayRangeTextColor = "#D0D0D0";
        _viewModel.OverlayMinMaxColor = "#2563B5";
        _viewModel.OverlayAcColor = "#C49A16";
        _viewModel.OverlayDcColor = "#228B50";
        _viewModel.OverlayHoldColor = "#D97706";
    }

    private void Cancel_Click(
        object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
