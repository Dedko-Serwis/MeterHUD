using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MultimeterOverlay.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Current => Instance;
    public static LocalizationService Instance { get; } = new();

    // Zasoby RESX pozostają zabezpieczeniem na wypadek braku lub błędu plików JSON.
    private readonly ResourceManager _resources = new(
        "MultimeterOverlay.Resources.Languages.Strings",
        typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("pl");
    private Dictionary<string, string> _polish = new();
    private Dictionary<string, string> _selected = new();

    private LocalizationService()
    {
        ReloadTranslations();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get
        {
            if (_selected.TryGetValue(key, out string? value))
            {
                return value;
            }

            if (_polish.TryGetValue(key, out value))
            {
                return value;
            }

            return _resources.GetString(key, _culture)
                ?? _resources.GetString(key, CultureInfo.GetCultureInfo("pl"))
                ?? key;
        }
    }

    public string Language => _culture.TwoLetterISOLanguageName;

    public void SetLanguage(string language)
    {
        string selected = language == "en" ? "en" : "pl";
        if (Language == selected)
        {
            return;
        }

        _culture = CultureInfo.GetCultureInfo(selected);
        ReloadTranslations();
        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(Language));
    }

    private void ReloadTranslations()
    {
        _polish = LoadTranslations("pl");
        _selected = Language == "pl" ? _polish : LoadTranslations(Language);
    }

    private static Dictionary<string, string> LoadTranslations(string language)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Languages", language + ".json");
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or NotSupportedException)
        {
            return new Dictionary<string, string>();
        }
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
