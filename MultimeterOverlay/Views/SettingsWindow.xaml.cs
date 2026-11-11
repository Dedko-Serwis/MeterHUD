
using MultimeterOverlay.Services;
using MultimeterOverlay.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;
using Forms = System.Windows.Forms;

namespace MultimeterOverlay.Views;

public partial class SettingsWindow : Window
{
    [GeneratedRegex(@"^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(
        @"^UT161E_\d{8}_\d{6}_\d{3}\.csv$",
        RegexOptions.IgnoreCase)]
    private static partial Regex CsvFileRegex();

    private readonly AppSettings _settings;
    private readonly SettingsViewModel _viewModel;
    private readonly Func<bool> _isRecording;

    public SettingsWindow(AppSettings settings, Func<bool>? isRecording = null)
    {
        InitializeComponent();

        _settings = settings;
        _isRecording = isRecording ?? (() => false);
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
            !TryNormalizeColor(_viewModel.OverlayRangeTextColor, out string rangeText) ||
            !TryNormalizeColor(_viewModel.OverlayMinMaxColor, out string minMax) ||
            !TryNormalizeColor(_viewModel.OverlayAcColor, out string ac) ||
            !TryNormalizeColor(_viewModel.OverlayDcColor, out string dc) ||
            !TryNormalizeColor(_viewModel.OverlayHoldColor, out string hold))
        {
            MessageBox.Show(LocalizationService.Instance["SettingsInvalidColor"], "MeterHUD",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string csvDirectory;
        try
        {
            if (string.IsNullOrWhiteSpace(_viewModel.CsvDirectory))
            {
                throw new ArgumentException(LocalizationService.Instance["SettingsSpecifyCsvFolder"]);
            }

            csvDirectory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(_viewModel.CsvDirectory.Trim()));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            MessageBox.Show(LocalizationService.Instance["SettingsInvalidCsvPath"], "MeterHUD",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string previousCsvDirectory = _settings.CsvDirectory;
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

            _settings.CsvDirectory = string.Equals(
                Path.TrimEndingDirectorySeparator(csvDirectory),
                Path.TrimEndingDirectorySeparator(UT161EReader.UT161EHidLogger.LogDirectory),
                StringComparison.OrdinalIgnoreCase)
                ? ""
                : csvDirectory;
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
            _settings.CsvDirectory = previousCsvDirectory;
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
                "MeterHUD",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool TryNormalizeColor(string? value, out string normalized)
    {
        normalized = "";

        if (value is null || !HexColorRegex().IsMatch(value))
        {
            return false;
        }

        normalized = value.ToUpperInvariant();
        return true;
    }

    private void RefreshColorPreviews()
    {
        if (TryNormalizeColor(_viewModel.OverlayBackgroundColor, out string bg))
        {
            BackgroundColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
        }

        if (TryNormalizeColor(_viewModel.OverlayTextColor, out string fg))
        {
            TextColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg));
        }

        if (TryNormalizeColor(_viewModel.OverlayConnectedColor, out string connected))
        {
            ConnectedColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(connected));
        }

        if (TryNormalizeColor(_viewModel.OverlayWaitingColor, out string waiting))
        {
            WaitingColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(waiting));
        }

        if (TryNormalizeColor(_viewModel.OverlayDisconnectedColor, out string disconnected))
        {
            DisconnectedColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(disconnected));
        }

        if (TryNormalizeColor(_viewModel.OverlayRangeTextColor, out string rangeText))
        {
            RangeTextColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(rangeText));
        }

        if (TryNormalizeColor(_viewModel.OverlayMinMaxColor, out string minMax))
        {
            MinMaxColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(minMax));
        }

        if (TryNormalizeColor(_viewModel.OverlayAcColor, out string ac))
        {
            AcColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ac));
        }

        if (TryNormalizeColor(_viewModel.OverlayDcColor, out string dc))
        {
            DcColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dc));
        }

        if (TryNormalizeColor(_viewModel.OverlayHoldColor, out string hold))
        {
            HoldColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hold));
        }
    }

    private static void PickColor(string current, Action<string> setColor)
    {
        using var dialog = new Forms.ColorDialog { FullOpen = true, AnyColor = true };
        if (TryNormalizeColor(current, out string hex))
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            dialog.Color = DrawingColor.FromArgb(color.R, color.G, color.B);
        }
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            setColor($"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}");
        }
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
        _viewModel.OverlayBackgroundColor = AppColors.OverlayBackground;
        _viewModel.OverlayTextColor = AppColors.OverlayText;
        _viewModel.OverlayConnectedColor = AppColors.OverlayConnected;
        _viewModel.OverlayWaitingColor = AppColors.OverlayWaiting;
        _viewModel.OverlayDisconnectedColor = AppColors.OverlayDisconnected;
        _viewModel.OverlayRangeTextColor = AppColors.OverlayRangeText;
        _viewModel.OverlayMinMaxColor = AppColors.OverlayMinMax;
        _viewModel.OverlayAcColor = AppColors.OverlayAc;
        _viewModel.OverlayDcColor = AppColors.OverlayDc;
        _viewModel.OverlayHoldColor = AppColors.OverlayHold;
    }

    private void BrowseCsvDirectory_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = LocalizationService.Instance["SettingsChooseCsvFolder"],
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_viewModel.CsvDirectory)
                ? _viewModel.CsvDirectory
                : _settings.GetCsvDirectory()
        };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _viewModel.CsvDirectory = dialog.SelectedPath;
        }
    }

    private void DefaultCsvDirectory_Click(object sender, RoutedEventArgs e) =>
        _viewModel.CsvDirectory = UT161EReader.UT161EHidLogger.LogDirectory;

    private void OpenCsvDirectory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string directory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(_viewModel.CsvDirectory.Trim()));
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance["SettingsCannotOpenCsv"], ex.Message),
                "MeterHUD", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteCsvFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording())
        {
            MessageBox.Show(LocalizationService.Instance["SettingsStopRecording"],
                "MeterHUD", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(_viewModel.CsvDirectory))
            {
                throw new ArgumentException(LocalizationService.Instance["SettingsNoFolder"]);
            }

            string directory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(_viewModel.CsvDirectory.Trim()));

            if (!Directory.Exists(directory))
            {
                MessageBox.Show(LocalizationService.Instance["SettingsFolderMissing"], "MeterHUD",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Wyłącznie pliki tworzone przez logger: UT161E_yyyyMMdd_HHmmss_fff.csv
            string[] files = [.. Directory.GetFiles(
                directory,
                "UT161E_*.csv",
                SearchOption.TopDirectoryOnly)
                .Where(path => CsvFileRegex().IsMatch(Path.GetFileName(path)))];

            if (files.Length == 0)
            {
                MessageBox.Show(LocalizationService.Instance["SettingsNoCsvFiles"],
                    "MeterHUD", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    string.Format(LocalizationService.Instance["SettingsDeleteConfirm"], files.Length, directory),
                    "MeterHUD", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            // Ponowna kontrola po potwierdzeniu.
            if (_isRecording())
            {
                MessageBox.Show(LocalizationService.Instance["SettingsRecordingActive"],
                    "MeterHUD", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int deleted = 0;
            int failed = 0;
            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                    or System.Security.SecurityException)
                {
                    failed++;
                }
            }

            MessageBox.Show(string.Format(LocalizationService.Instance["SettingsDeleteResult"], deleted, failed),
                "MeterHUD", MessageBoxButton.OK,
                failed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance["SettingsDeleteError"], ex.Message),
                "MeterHUD", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(
        object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}