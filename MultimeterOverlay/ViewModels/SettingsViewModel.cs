
using MultimeterOverlay.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultimeterOverlay.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string _language;
    private string _overlayBackgroundColor;
    private string _overlayTextColor;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(AppSettings settings)
    {
        _language = settings.Language is "pl" or "en"
            ? settings.Language
            : "pl";
        _overlayBackgroundColor = settings.OverlayBackgroundColor;
        _overlayTextColor = settings.OverlayTextColor;
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

    public string OverlayBackgroundColor
    {
        get => _overlayBackgroundColor;
        set { if (_overlayBackgroundColor == value) return; _overlayBackgroundColor = value; OnPropertyChanged(); }
    }

    public string OverlayTextColor
    {
        get => _overlayTextColor;
        set { if (_overlayTextColor == value) return; _overlayTextColor = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
