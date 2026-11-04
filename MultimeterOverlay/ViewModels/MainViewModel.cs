
using MultimeterOverlay.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using UT161EReader;

namespace MultimeterOverlay.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private UT161EMultimeter? _multimeter;
    private CancellationTokenSource? _cts;
    private Task? _readingTask;

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
        if (_readingTask is not null)
            return;

        try
        {
            _multimeter = new UT161EMultimeter();
            _cts = new CancellationTokenSource();

            DeviceName = _multimeter.Name;
            Status = "Połączono";

            _readingTask = ReadLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Status = "Błąd połączenia";
            Function = ex.Message;
        }
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        if (_multimeter is null)
            return;

        try
        {
            while (!token.IsCancellationRequested)
            {
                MultimeterReading? reading;

                try
                {
                    reading = await _multimeter.ReadAsync(token);
                }
                catch (TimeoutException)
                {
                    Status = "Oczekiwanie na miernik...";
                    continue;
                }

                if (token.IsCancellationRequested)
                    break;

                if (reading is not null)
                {
                    DisplayValue = reading.DisplayValue;
                    Unit = reading.Unit;
                    Function = reading.Function;
                    Range = reading.IsAutoRange ? "AUTO" : "MANUAL";
                    IsHold = reading.IsHold;
                    Status = "Połączono";
                }
                else
                {
                    Status = "Brak danych";
                }

                await Task.Delay(200, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normalne zakończenie.
        }
        catch (Exception ex)
        {
            Status = "Błąd odczytu";
            Function = ex.Message;
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_readingTask is not null)
        {
            try
            {
                await _readingTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _multimeter?.Dispose();
        _cts?.Dispose();

        _multimeter = null;
        _cts = null;
        _readingTask = null;

        Status = "Rozłączono";
    }

    public void Dispose()
    {
        _cts?.Cancel();

        // Dispose nie zastępuje StopAsync.
        // StopAsync należy wywołać przed Dispose.
        _cts?.Dispose();
        _multimeter?.Dispose();
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
