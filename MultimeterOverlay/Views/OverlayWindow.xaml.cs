
using MultimeterOverlay.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace MultimeterOverlay.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    protected override void OnMouseLeftButtonDown(
        MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
