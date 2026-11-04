
using MultimeterOverlay.ViewModels;
using MultimeterOverlay.Views;
using System.Windows;

namespace MultimeterOverlay;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private OverlayWindow? _overlayWindow;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
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
        _overlayWindow?.Close();

        await _viewModel.StopAsync();
        _viewModel.Dispose();
    }
}
