
using MeterHUD.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeterHUD.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string _language;
    private string _csvDirectory;
    private string _overlayBackgroundColor;
    private string _overlayTextColor;
    private string _overlayConnectedColor;
    private string _overlayWaitingColor;
    private string _overlayDisconnectedColor;
    private string _overlayRangeTextColor;
    private string _overlayMinMaxColor;
    private string _overlayAcColor;
    private string _overlayDcColor;
    private string _overlayHoldColor;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(AppSettings settings)
    {
        _language = settings.Language is "pl" or "en"
            ? settings.Language
            : "pl";
        _csvDirectory = settings.GetCsvDirectory();
        _overlayBackgroundColor = settings.OverlayBackgroundColor;
        _overlayTextColor = settings.OverlayTextColor;
        _overlayConnectedColor = settings.OverlayConnectedColor;
        _overlayWaitingColor = settings.OverlayWaitingColor;
        _overlayDisconnectedColor = settings.OverlayDisconnectedColor;
        _overlayRangeTextColor = settings.OverlayRangeTextColor;
        _overlayMinMaxColor = settings.OverlayMinMaxColor;
        _overlayAcColor = settings.OverlayAcColor;
        _overlayDcColor = settings.OverlayDcColor;
        _overlayHoldColor = settings.OverlayHoldColor;
    }

    public string CsvDirectory
    {
        get => _csvDirectory;
        set { if (_csvDirectory == value) { return; } _csvDirectory = value; OnPropertyChanged(); }
    }

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            OnPropertyChanged();
        }
    }

    public string OverlayBackgroundColor
    {
        get => _overlayBackgroundColor;
        set { if (_overlayBackgroundColor == value) { return; } _overlayBackgroundColor = value; OnPropertyChanged(); }
    }

    public string OverlayTextColor
    {
        get => _overlayTextColor;
        set { if (_overlayTextColor == value) { return; } _overlayTextColor = value; OnPropertyChanged(); }
    }

    public string OverlayConnectedColor
    {
        get => _overlayConnectedColor;
        set { if (_overlayConnectedColor == value) { return; } _overlayConnectedColor = value; OnPropertyChanged(); }
    }

    public string OverlayWaitingColor
    {
        get => _overlayWaitingColor;
        set { if (_overlayWaitingColor == value) { return; } _overlayWaitingColor = value; OnPropertyChanged(); }
    }

    public string OverlayDisconnectedColor
    {
        get => _overlayDisconnectedColor;
        set { if (_overlayDisconnectedColor == value) { return; } _overlayDisconnectedColor = value; OnPropertyChanged(); }
    }

    public string OverlayRangeTextColor
    {
        get => _overlayRangeTextColor;
        set { if (_overlayRangeTextColor == value) { return; } _overlayRangeTextColor = value; OnPropertyChanged(); }
    }

    public string OverlayMinMaxColor
    {
        get => _overlayMinMaxColor;
        set { if (_overlayMinMaxColor == value) { return; } _overlayMinMaxColor = value; OnPropertyChanged(); }
    }

    public string OverlayAcColor
    {
        get => _overlayAcColor;
        set { if (_overlayAcColor == value) { return; } _overlayAcColor = value; OnPropertyChanged(); }
    }

    public string OverlayDcColor
    {
        get => _overlayDcColor;
        set { if (_overlayDcColor == value) { return; } _overlayDcColor = value; OnPropertyChanged(); }
    }

    public string OverlayHoldColor
    {
        get => _overlayHoldColor;
        set { if (_overlayHoldColor == value) { return; } _overlayHoldColor = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
