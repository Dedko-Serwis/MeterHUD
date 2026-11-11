
namespace MeterHUD.Core;

/// <summary>
/// Wspólny interfejs sterowników multimetrów.
/// </summary>
public interface IMultimeter : IDisposable
{
    /// <summary>
    /// Nazwa urządzenia, np. UNI-T UT161E.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Informacja o aktywnym połączeniu.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Odczytuje pojedynczy pomiar.
    /// Null oznacza brak poprawnego pomiaru.
    /// </summary>
    Task<MultimeterReading?> ReadAsync(
        CancellationToken cancellationToken);
}
