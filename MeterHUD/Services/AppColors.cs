
using System.Windows.Media;

namespace MeterHUD.Services;

/// <summary>
/// Centralna definicja kolorów nakładki i interfejsu aplikacji.
/// </summary>
public static class AppColors
{
    // ============================================================
    // KOLORY NAKŁADKI – MODYFIKOWANE PRZEZ UŻYTKOWNIKA
    // Wartości domyślne. Własne ustawienia zapisane są w settings.json.
    // ============================================================

    public const string OverlayBackground = "#181818";    // Tło okna pomiarowego i nakładki OBS
    public const string OverlayText = "#FFFFFF";          // Główne cyfry pomiaru i jednostki
    public const string OverlayConnected = "#00B72E";     // Ramka informująca o połączeniu z miernikiem
    public const string OverlayWaiting = "#E0D725";       // Ramka podczas oczekiwania na połączenie
    public const string OverlayDisconnected = "#585858";  // Ramka przy braku połączenia
    public const string OverlayRangeText = "#80FF80";     // Tekst zakresu pomiarowego
    public const string OverlayMinMax = "#0071E1";        // Aktywne wskaźniki MIN i MAX
    public const string OverlayAc = "#FF0000";            // Aktywny wskaźnik AC
    public const string OverlayDc = "#00C400";            // Aktywny wskaźnik DC
    public const string OverlayHold = "#FF8000";          // Aktywny wskaźnik HOLD

    // ============================================================
    // KONWERSJA DOMYŚLNYCH KOLORÓW NAKŁADKI NA TYP WPF COLOR
    // Wykorzystywane przez zasoby aplikacji (App.xaml).
    // ============================================================

    public static Color OverlayBackgroundValue => Parse(OverlayBackground);
    public static Color OverlayTextValue => Parse(OverlayText);
    public static Color OverlayConnectedValue => Parse(OverlayConnected);
    public static Color OverlayWaitingValue => Parse(OverlayWaiting);
    public static Color OverlayDisconnectedValue => Parse(OverlayDisconnected);
    public static Color OverlayRangeTextValue => Parse(OverlayRangeText);
    public static Color OverlayMinMaxValue => Parse(OverlayMinMax);
    public static Color OverlayAcValue => Parse(OverlayAc);
    public static Color OverlayDcValue => Parse(OverlayDc);
    public static Color OverlayHoldValue => Parse(OverlayHold);

    // ============================================================
    // KOLORY INTERFEJSU APLIKACJI – STAŁE
    // Nie są modyfikowane przez użytkownika w ustawieniach.
    // ============================================================

    public static SolidColorBrush Ui202020Brush { get; } = CreateBrush("#202020"); // Tło wskaźników AC/DC/MIN/MAX/HOLD oraz paneli informacyjnych
    public static SolidColorBrush Ui777777Brush { get; } = CreateBrush("#777777"); // Tekst nieaktywnych wskaźników nakładki OBS
    public static SolidColorBrush Ui555555Brush { get; } = CreateBrush("#555555"); // Tekst nieaktywnych wskaźników i obramowania paneli
    public static SolidColorBrush Ui252525Brush { get; } = CreateBrush("#252525"); // Tło menu głównego i paska elementów sterujących
    public static SolidColorBrush UiDDDDDDBrush { get; } = CreateBrush("#DDDDDD"); // Tekst na pasku elementów sterujących
    public static SolidColorBrush Ui444444Brush { get; } = CreateBrush("#444444"); // Obramowania elementów oraz tło wybranych paneli
    public static SolidColorBrush Ui333333Brush { get; } = CreateBrush("#333333"); // Kolor tekstu określonych elementów w zależności od stanu
    public static SolidColorBrush UiAAAAAABrush { get; } = CreateBrush("#AAAAAA"); // Tekst pomocniczy, nagłówki sekcji i opisy w oknach
    public static SolidColorBrush Ui27618ABrush { get; } = CreateBrush("#27618A"); // Obramowanie niebieskiego panelu informacyjnego
    public static SolidColorBrush Ui10304ABrush { get; } = CreateBrush("#10304A"); // Tło niebieskiego panelu informacyjnego
    public static SolidColorBrush Ui888888Brush { get; } = CreateBrush("#888888"); // Tekst pomocniczy w panelu informacyjnym
    public static SolidColorBrush Ui999999Brush { get; } = CreateBrush("#999999"); // Tekst drugorzędny w panelu informacyjnym
    public static SolidColorBrush Ui666666Brush { get; } = CreateBrush("#666666"); // Tekst elementów o obniżonym wyróżnieniu
    public static SolidColorBrush Ui181818Brush { get; } = CreateBrush("#181818"); // Tło okna ustawień i okna O programie
    public static SolidColorBrush UiWhiteBrush { get; } = CreateBrush("#FFFFFF");  // Biały tekst interfejsu i aktywnych wskaźników
    public static SolidColorBrush UiBlackBrush { get; } = CreateBrush("#000000");  // Tekst pozycji rozwijanego menu
    public static SolidColorBrush UiOrangeRedBrush { get; } = CreateBrush("#FF4500"); // Wskaźnik aktywnego nagrywania CSV
    public static SolidColorBrush UiRedBrush { get; } = CreateBrush("#FF0000");    // Czerwony wskaźnik stanu połączenia
    public static SolidColorBrush UiLimeGreenBrush { get; } = CreateBrush("#32CD32"); // Zielony wskaźnik stanu połączenia
    public static SolidColorBrush UiGrayBrush { get; } = CreateBrush("#808080");   // Szary wskaźnik stanu połączenia

    // ============================================================
    // METODY POMOCNICZE
    // ============================================================

    /// <summary>
    /// Sprawdza poprawność koloru HEX (#RRGGBB).
    /// Jeśli kolor jest nieprawidłowy, używa wartości domyślnej.
    /// </summary>
    public static SolidColorBrush ParseOverlayBrush(string? value, string fallback)
    {
        if (value is null || value.Length != 7 || value[0] != '#' ||
            !value.AsSpan(1).ToString().All(Uri.IsHexDigit))
            value = fallback;

        return new SolidColorBrush(Parse(value));
    }

    /// <summary>
    /// Konwertuje zapis HEX na kolor WPF.
    /// </summary>
    private static Color Parse(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex);

    /// <summary>
    /// Tworzy pędzel WPF i blokuje jego modyfikację.
    /// </summary>
    private static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush(Parse(hex));
        brush.Freeze();
        return brush;
    }
}
