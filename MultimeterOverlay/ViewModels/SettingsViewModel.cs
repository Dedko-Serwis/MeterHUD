
using MultimeterOverlay.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultimeterOverlay.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string _language;
    private string _overlayBackgroundColor;
    private string _overlayTextColor;
    private string _overlayConnectedColor;
    private string _overlayWaitingColor;
    private string _overlayDisconnectedColor;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(AppSettings settings)
    {
        _language = settings.Language is "pl" or "en"
            ? settings.Language
            : "pl";
        _overlayBackgroundColor = settings.OverlayBackgroundColor;
        _overlayTextColor = settings.OverlayTextColor;
        _overlayConnectedColor = settings.OverlayConnectedColor;
        _overlayWaitingColor = settings.OverlayWaitingColor;
        _overlayDisconnectedColor = settings.OverlayDisconnectedColor;
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

    public string OverlayConnectedColor
    {
        get => _overlayConnectedColor;
        set { if (_overlayConnectedColor == value) return; _overlayConnectedColor = value; OnPropertyChanged(); }
    }

    public string OverlayWaitingColor
    {
        get => _overlayWaitingColor;
        set { if (_overlayWaitingColor == value) return; _overlayWaitingColor = value; OnPropertyChanged(); }
    }

    public string OverlayDisconnectedColor
    {
        get => _overlayDisconnectedColor;
        set { if (_overlayDisconnectedColor == value) return; _overlayDisconnectedColor = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
