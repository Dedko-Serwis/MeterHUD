
using MultimeterOverlay.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultimeterOverlay.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string _language;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(AppSettings settings)
    {
        _language = settings.Language is "pl" or "en"
            ? settings.Language
            : "pl";
    }

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value)
                return;

            _language = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
