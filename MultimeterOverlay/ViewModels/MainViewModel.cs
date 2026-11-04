
using MultimeterOverlay.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UT161EReader;

namespace MultimeterOverlay.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int ReconnectDelayMs = 2000;
    private const int ReadDelayMs = 200;
    private const int MaxConsecutiveTimeouts = 3;

    private UT161EMultimeter? _multimeter;
    private CancellationTokenSource? _cts;
    private Task? _readingTask;
    private bool _disposed;

    private string _deviceName = "UNI-T UT161E";
    private string _displayValue = "----";
    private string _unit = "";
    private string _function = "Oczekiwanie na pomiar";
    private string _range = "";
    private string _status = "Niepołączono";
    private bool _isHold;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DeviceName
    {
        get => _deviceName;
        private set => SetField(ref _deviceName, value);
    }

    public string DisplayValue
    {
        get => _displayValue;
        private set => SetField(ref _displayValue, value);
    }

    public string Unit
    {
        get => _unit;
        private set => SetField(ref _unit, value);
    }

    public string Function
    {
        get => _function;
        private set => SetField(ref _function, value);
    }

    public string Range
    {
        get => _range;
        private set => SetField(ref _range, value);
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public bool IsHold
    {
        get => _isHold;
        private set => SetField(ref _isHold, value);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_readingTask is not null)
            return;

        _cts = new CancellationTokenSource();
        _readingTask = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_multimeter is null)
                {
                    Status = "Łączenie...";

                    try
                    {
                        _multimeter = new UT161EMultimeter();
                        DeviceName = _multimeter.Name;
                        Status = "Połączono";
                    }
                    catch (Exception ex)
                    {
                        Status = "Miernik niedostępny";
                        Function = ex.Message;

                        await Task.Delay(
                            ReconnectDelayMs, token);

                        continue;
                    }
                }

                int timeoutCount = 0;
                bool reconnect = false;

                while (!token.IsCancellationRequested &&
                       !reconnect)
                {
                    try
                    {
                        MultimeterReading? reading =
                            await _multimeter.ReadAsync(token);

                        if (token.IsCancellationRequested)
                            break;

                        timeoutCount = 0;

                        if (reading is not null)
                        {
                            UpdateReading(reading);
                            Status = "Połączono";
                        }
                        else
                        {
                            Status = "Brak danych";
                        }

                        await Task.Delay(ReadDelayMs, token);
                    }
                    catch (TimeoutException)
                    {
                        timeoutCount++;

                        Status = "Oczekiwanie na miernik...";

                        if (timeoutCount >= MaxConsecutiveTimeouts)
                        {
                            Status = "Brak odpowiedzi miernika";
                            reconnect = true;
                        }
                    }
                    catch (OperationCanceledException)
                        when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Status = "Utracono połączenie";
                        Function = ex.Message;
                        reconnect = true;
                    }
                }

                Disconnect();

                if (!token.IsCancellationRequested)
                {
                    ClearReading();

                    await Task.Delay(
                        ReconnectDelayMs, token);
                }
            }
        }
        catch (OperationCanceledException)
            when (token.IsCancellationRequested)
        {
            // Normalne zatrzymanie.
        }
        finally
        {
            Disconnect();
        }
    }

    private void UpdateReading(MultimeterReading reading)
    {
        DisplayValue = reading.DisplayValue;
        Unit = reading.Unit;
        Function = reading.Function;
        Range = reading.IsAutoRange ? "AUTO" : "MANUAL";
        IsHold = reading.IsHold;
    }

    private void ClearReading()
    {
        DisplayValue = "----";
        Unit = "";
        Range = "";
        IsHold = false;
    }

    private void Disconnect()
    {
        _multimeter?.Dispose();
        _multimeter = null;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();

        if (_readingTask is not null)
        {
            try
            {
                await _readingTask;
            }
            catch (OperationCanceledException)
            {
                // Normalne zatrzymanie.
            }
        }

        _readingTask = null;

        _cts.Dispose();
        _cts = null;

        ClearReading();
        Status = "Rozłączono";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_readingTask is not null)
        {
            throw new InvalidOperationException(
                "Przed Dispose należy wywołać StopAsync.");
        }

        _disposed = true;
        _cts?.Dispose();
        Disconnect();

        GC.SuppressFinalize(this);
    }

    private void SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
