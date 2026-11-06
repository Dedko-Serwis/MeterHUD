
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace MultimeterOverlay.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Current => Instance;
    public static LocalizationService Instance { get; } = new();

    private readonly ResourceManager _resources = new(
        "MultimeterOverlay.Resources.Languages.Strings",
        typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("pl");

    private LocalizationService()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] =>
        _resources.GetString(key, _culture) ?? key;

    public string Language => _culture.TwoLetterISOLanguageName;

    public void SetLanguage(string language)
    {
        string selected = language == "en" ? "en" : "pl";

        if (Language == selected)
            return;

        _culture = CultureInfo.GetCultureInfo(selected);

        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(Language));
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
