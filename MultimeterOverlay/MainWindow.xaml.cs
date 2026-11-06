
using MultimeterOverlay.ViewModels;
using MultimeterOverlay.Views;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MultimeterOverlay;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private OverlayWindow? _overlayWindow;
    private readonly DispatcherTimer _recordingTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };

    private bool _blinkVisible = true;

    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        _recordingTimer.Tick += RecordingTimer_Tick;
        _recordingTimer.Start();


        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
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

        _overlayWindow = new OverlayWindow(_viewModel)
        {
            Owner = this
        };

        _overlayWindow.Show();
    }

    private async void MainWindow_Closed(
        object? sender, EventArgs e)
    {
        _recordingTimer.Stop();
        _overlayWindow?.Close();

        await _viewModel.StopAsync();
        _viewModel.Dispose();
    }
}
