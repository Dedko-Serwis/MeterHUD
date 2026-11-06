
using MultimeterOverlay.Services;
using MultimeterOverlay.ViewModels;
using System.Windows;

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
    }

    private void Save_Click(
        object sender, RoutedEventArgs e)
    {
        string previousLanguage = _settings.Language;

        try
        {
            _settings.Language = _viewModel.Language;
            _settings.Save();

            DialogResult = true;
        }
        catch (Exception ex)
        {
            _settings.Language = previousLanguage;

            MessageBox.Show(
                $"Nie można zapisać ustawień:\n{ex.Message}",
                "MultimeterOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(
        object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
