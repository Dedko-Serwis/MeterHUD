
using MultimeterOverlay.ViewModels;
using MultimeterOverlay.Views;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MultimeterOverlay.Services;
using System.Diagnostics;
using System.IO;
using UT161EReader;

namespace MultimeterOverlay;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private OverlayWindow? _overlayWindow;
    private readonly AppSettings _settings = AppSettings.Load();
    private bool _simulationSwitchInProgress;
    private bool _displayTestSwitchInProgress;
    private readonly DispatcherTimer _recordingTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };

    private bool _blinkVisible = true;

    public MainWindow()
    {
        InitializeComponent();
        LocalizationService.Instance.SetLanguage(_settings.Language);
        RestoreMainWindow();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        _recordingTimer.Tick += RecordingTimer_Tick;
        _recordingTimer.Start();


        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }


    private async void DisplayTestMenuItem_Click(
        object sender, RoutedEventArgs e)
    {
        if (_displayTestSwitchInProgress)
            return;

        _displayTestSwitchInProgress = true;

        DisplayTestMenuItem.IsEnabled = false;
        SimulationMenuItem.IsEnabled = false;

        try
        {
            if (DisplayTestMenuItem.IsChecked)
            {
                await _viewModel.EnableDisplayTestAsync();
            }
            else
            {
                _viewModel.DisableDisplayTest();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd testu wyświetlacza:\n{ex.Message}",
                "MultimeterOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            DisplayTestMenuItem.IsChecked =
                _viewModel.IsDisplayTestMode;

            SimulationMenuItem.IsChecked =
                _viewModel.IsSimulationMode &&
                !_viewModel.IsDisplayTestMode;

            SimulationMenuItem.IsEnabled = true;
            DisplayTestMenuItem.IsEnabled = true;

            _displayTestSwitchInProgress = false;
        }
    }


    private async void SimulationMenuItem_Click(
        object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsDisplayTestMode)            return;
        if (_simulationSwitchInProgress)
        {
            SimulationMenuItem.IsChecked = _viewModel.IsSimulationMode;
            return;
        }

        _simulationSwitchInProgress = true;
        SimulationMenuItem.IsEnabled = false;

        bool enable = SimulationMenuItem.IsChecked;

        try
        {
            if (enable)
            {
                await _viewModel.EnableSimulationAsync();
            }
            else
            {
                _viewModel.DisableSimulation();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd przełączania symulacji:\n{ex.Message}",
                "MultimeterOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SimulationMenuItem.IsChecked =
                _viewModel.IsSimulationMode;

            SimulationMenuItem.IsEnabled = true;
            _simulationSwitchInProgress = false;
        }
    }


    private void MainWindow_PreviewKeyDown(
    object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F12)
            return;

        _viewModel.ToggleRecording();

        _blinkVisible = true;
        UpdateRecordingIndicator();

        e.Handled = true;
    }

    private void SaveMainWindow()
    {
        Rect bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        _settings.MainLeft = bounds.Left;
        _settings.MainTop = bounds.Top;
        _settings.MainWidth = bounds.Width;
        _settings.MainHeight = bounds.Height;

        _settings.MainMaximized =
            WindowState == WindowState.Maximized;
    }


    private void RecordingTimer_Tick(
        object? sender, EventArgs e)
    {
        _viewModel.RefreshRecordingCounter();

        _blinkVisible = !_blinkVisible;
        UpdateRecordingIndicator();
    }

    private void UpdateRecordingIndicator()
    {
        if (_viewModel.IsRecording)
        {
            RecordingIndicator.Foreground = Brushes.OrangeRed;

            RecordingIndicator.Opacity =
                _blinkVisible ? 1.0 : 0.15;
        }
        else
        {
            RecordingIndicator.Opacity = 1.0;

            RecordingIndicator.Foreground =
                _viewModel.RecordingStatus == "ERROR"
                    ? Brushes.Red
                    : _viewModel.HasRecording
                        ? Brushes.LimeGreen
                        : Brushes.Gray;
        }
    }


    private void MainWindow_Loaded(
        object sender, RoutedEventArgs e)
    {
        _viewModel.Start();

        _overlayWindow = new OverlayWindow(_viewModel, _settings)
        {
            Owner = this
        };

        ShowOverlayMenuItem.IsChecked = _settings.ShowOverlay;

        if (_settings.ShowOverlay)
            _overlayWindow.Show();
    }


    private void ShowOverlayMenuItem_Click(
        object sender, RoutedEventArgs e)
    {
        if (_overlayWindow is null)
            return;

        bool show = ShowOverlayMenuItem.IsChecked;

        _settings.ShowOverlay = show;

        if (show)
            _overlayWindow.Show();
        else
        {
            _overlayWindow.SavePosition();
            _overlayWindow.Hide();
        }

        SaveSettings();
    }

    private void OpenCsvFolder_Click(
        object sender, RoutedEventArgs e)
    {
        try
        {
            string directory = UT161EHidLogger.LogDirectory;

            Directory.CreateDirectory(directory);

            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie można otworzyć folderu nagrań:\n{ex.Message}",
                "MultimeterOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ResetMainWindow()
    {
        WindowState = WindowState.Normal;

        Width = 560;
        Height = 300;

        Rect workArea = SystemParameters.WorkArea;

        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    private void ResetSettings_Click(
    object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            "Przywrócić domyślne ustawienia aplikacji?\n\n" +
            "Pliki CSV nie zostaną usunięte.",
            "Reset ustawień",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            _overlayWindow?.ResetPosition();
            ResetMainWindow();

            _settings.ShowOverlay = true;
            ShowOverlayMenuItem.IsChecked = true;

            _overlayWindow?.Show();

            SaveSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie można zresetować ustawień:\n{ex.Message}",
                "MultimeterOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void SaveSettings()
    {
        try
        {
            SaveMainWindow();
            _overlayWindow?.SavePosition();
            _settings.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie można zapisać ustawień:\n{ex.Message}", "MultimeterOverlay", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }



    private async void MainWindow_Closed(
        object? sender, EventArgs e)
    {
        _recordingTimer.Stop();
        SaveSettings();
        _overlayWindow?.Close();

        if (_viewModel.IsDisplayTestMode) _viewModel.DisableDisplayTest();
        if (_viewModel.IsSimulationMode) _viewModel.DisableSimulation(restartDevice: false);

        await _viewModel.StopAsync();
        _viewModel.Dispose();
    }

    private void SettingsMenuItem_Click(
        object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_settings)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void RestoreMainWindow()
    {
        double width = _settings.MainWidth;
        double height = _settings.MainHeight;

        if (!double.IsFinite(width) ||
            !double.IsFinite(height) ||
            width < MinWidth ||
            height < MinHeight ||
            width > SystemParameters.VirtualScreenWidth ||
            height > SystemParameters.VirtualScreenHeight)
        {
            return;
        }

        Width = width;
        Height = height;

        if (_settings.MainLeft is double left &&
            _settings.MainTop is double top &&
            double.IsFinite(left) &&
            double.IsFinite(top))
        {
            Rect savedBounds = new(left, top, width, height);

            Rect screen = new(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);

            if (savedBounds.IntersectsWith(screen))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = left;
                Top = top;
            }
        }

        if (_settings.MainMaximized)
            WindowState = WindowState.Maximized;
    }
}