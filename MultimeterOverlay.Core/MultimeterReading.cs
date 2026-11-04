
namespace MultimeterOverlay.Core;

/// <summary>
/// Uniwersalny model pojedynczego pomiaru.
/// Niezależny od producenta i modelu multimetru.
/// </summary>
public sealed record MultimeterReading
{
    /// <summary>
    /// Wartość pomiaru przeliczona na jednostkę podstawową SI.
    /// Null oznacza brak dostępnej wartości liczbowej.
    /// </summary>
    public double? Value { get; init; }

    /// <summary>
    /// Wartość dokładnie w postaci przeznaczonej do wyświetlenia,
    /// np. "12.345", "-0.125", "OL".
    /// </summary>
    public string DisplayValue { get; init; } = "";

    /// <summary>
    /// Jednostka wyświetlana, np. V DC, mV AC, kΩ, µF.
    /// </summary>
    public string Unit { get; init; } = "";

    /// <summary>
    /// Rodzaj pomiaru, np. Napięcie DC, Rezystancja.
    /// </summary>
    public string Function { get; init; } = "";

    /// <summary>
    /// Przekroczenie zakresu pomiarowego.
    /// </summary>
    public bool IsOverload { get; init; }

    /// <summary>
    /// Zatrzymanie wskazania (HOLD).
    /// </summary>
    public bool IsHold { get; init; }

    /// <summary>
    /// Automatyczny wybór zakresu.
    /// </summary>
    public bool IsAutoRange { get; init; }

    /// <summary>
    /// Czas odebrania pomiaru.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
