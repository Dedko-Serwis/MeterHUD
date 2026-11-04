
using MultimeterOverlay.ViewModels;
using System.Windows;

namespace MultimeterOverlay;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;

        Loaded += (_, _) => _viewModel.Start();
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Closed(
        object? sender, EventArgs e)
    {
        await _viewModel.StopAsync();
        _viewModel.Dispose();
    }
}
