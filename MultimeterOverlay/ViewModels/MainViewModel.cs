
using MultimeterOverlay.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UT161EReader;

namespace MultimeterOverlay.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int ReconnectDelayMs = 2000;
    private const int ReadDelayMs = 200;
    private static readonly TimeSpan NoDataTimeout =  TimeSpan.FromSeconds(5);


    private UT161EMultimeter? _multimeter;
    private CancellationTokenSource? _cts;
    private Task? _readingTask;
    private bool _disposed;

    private string _deviceName = "UNI-T UT161E";
    private string _displayValue = "----";
    private string _unit = "";
    private string _function = "Oczekiwanie na pomiar";
    private string _range = "";
    private string _status = "Brak komunikacji";
    private bool _isHold;
    private int _recordedFrames;
    private string _recordingStatus = "READY";
    private bool _isRecording;
    private bool _hasRecording;
    private bool _isMin;
    private bool _isMax;
    private bool _isAcDcMode;
    private string _acDisplayValue = "----";
    private string _dcDisplayValue = "----";
    private string _acDcUnit = "V";
    private const int MaxConsecutiveTimeouts = 3;
    private int _consecutiveTimeouts;
    private bool _hasReceivedReading;

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

    public int RecordedFrames
    {
        get => _recordedFrames;
        private set => SetField(ref _recordedFrames, value);
    }

    public string RecordingStatus
    {
        get => _recordingStatus;
        private set => SetField(ref _recordingStatus, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        private set => SetField(ref _isRecording, value);
    }

    public bool HasRecording
    {
        get => _hasRecording;
        private set => SetField(ref _hasRecording, value);
    }
    public bool IsAcDcMode
    {
        get => _isAcDcMode;
        private set => SetField(ref _isAcDcMode, value);
    }

    public string AcDisplayValue
    {
        get => _acDisplayValue;
        private set => SetField(ref _acDisplayValue, value);
    }

    public string DcDisplayValue
    {
        get => _dcDisplayValue;
        private set => SetField(ref _dcDisplayValue, value);
    }

    public string AcDcUnit
    {
        get => _acDcUnit;
        private set => SetField(ref _acDcUnit, value);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_readingTask is not null)
            return;

        _cts = new CancellationTokenSource();
        _readingTask = RunAsync(_cts.Token);
    }


    public bool IsAutoRange => Range == "AUTO";

    public bool IsManualRange => Range == "MANUAL";


    public bool IsDC =>
        Function.Contains("DC", StringComparison.OrdinalIgnoreCase)
        || Function.Contains("stały", StringComparison.OrdinalIgnoreCase);

    public bool IsAC =>
        Function.Contains("AC", StringComparison.OrdinalIgnoreCase)
        || Function.Contains("przemienny", StringComparison.OrdinalIgnoreCase);


    public bool IsMin
    {
        get => _isMin;
        private set => SetField(ref _isMin, value);
    }

    public bool IsMax
    {
        get => _isMax;
        private set => SetField(ref _isMax, value);
    }

    public string MeasurementType
    {
        get
        {
            if (Function.Contains("Napięcie", StringComparison.OrdinalIgnoreCase))
                return "Napięcie";

            if (Function.Contains("Prąd", StringComparison.OrdinalIgnoreCase))
                return "Prąd";

            if (Function.Contains("Rezystancja", StringComparison.OrdinalIgnoreCase))
                return "Rezystancja";

            if (Function.Contains("Pojemność", StringComparison.OrdinalIgnoreCase))
                return "Pojemność";

            if (Function.Contains("Częstotliwość", StringComparison.OrdinalIgnoreCase))
                return "Częstotliwość";

            return Function;
        }
    }

    public string OverlayUnit
    {
        get
        {
            return Unit
                .Replace("AC", "", StringComparison.OrdinalIgnoreCase)
                .Replace("DC", "", StringComparison.OrdinalIgnoreCase)
                .Trim();
        }
    }





    private async Task RunAsync(CancellationToken token)
    {
        Status = "Brak komunikacji";

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_multimeter is null)
                {
                    try
                    {
                        _multimeter = new UT161EMultimeter();
                        DeviceName = _multimeter.Name;
                    }
                    catch (Exception)
                    {
                        // Urządzenie HID jest niedostępne.
                        // Nie zmieniamy statusu na "Połączono".
                        Status = "Brak komunikacji";

                        await Task.Delay(ReconnectDelayMs, token);
                        continue;
                    }
                }

                bool reconnect = false;

                while (!token.IsCancellationRequested && !reconnect)
                {
                    try
                    {
                        MultimeterReading? reading =
                            await _multimeter.ReadAsync(token);

                        if (token.IsCancellationRequested)
                            break;

                        if (reading is not null)
                        {
                            _consecutiveTimeouts = 0;
                            _hasReceivedReading = true;

                            UpdateReading(reading);
                            Status = "Połączono";
                        }
                        else
                        {
                            HandleMissingReading();
                        }

                        await Task.Delay(ReadDelayMs, token);
                    }
                    catch (TimeoutException)
                    {
                        HandleMissingReading();

                        if (_consecutiveTimeouts >= MaxConsecutiveTimeouts)
                            reconnect = true;
                    }
                    catch (OperationCanceledException)
                        when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // Błąd komunikacji lub odłączenie urządzenia HID.
                        StopRecording();
                        ClearReading();

                        _consecutiveTimeouts = 0;
                        _hasReceivedReading = false;

                        Status = "Brak komunikacji";
                        reconnect = true;
                    }
                }

                Disconnect();

                if (!token.IsCancellationRequested)
                {
                    await Task.Delay(ReconnectDelayMs, token);
                }
            }
        }
        catch (OperationCanceledException)
            when (token.IsCancellationRequested)
        {
            // Normalne zatrzymanie aplikacji.
        }
        finally
        {
            Disconnect();
        }
    }

    private void HandleMissingReading()
    {
        _consecutiveTimeouts++;

        if (_consecutiveTimeouts >= MaxConsecutiveTimeouts)
        {
            StopRecording();
            ClearReading();

            _hasReceivedReading = false;
            Status = "Brak komunikacji";
        }
        else if (_hasReceivedReading)
        {
            Status = "Oczekiwanie na miernik";
        }
        else
        {
            Status = "Brak komunikacji";
        }
    }



    public void ToggleRecording()
    {
        if (IsRecording)
        {
            StopRecording();
            return;
        }

        if (_multimeter is null || Status != "Połączono")
            return;

        var logger = _multimeter.Logger;
        logger.Start();

        RecordedFrames = logger.FrameCount;
        IsRecording = logger.IsRecording;

        if (logger.LastError is not null)
        {
            RecordingStatus = "ERROR";
            HasRecording = false;
            return;
        }

        HasRecording = false;
        RecordingStatus = IsRecording ? "REC" : "READY";
    }

    public void StopRecording()
    {
        if (!IsRecording)
            return;

        if (_multimeter is not null)
        {
            var logger = _multimeter.Logger;
            logger.Stop();
            RecordedFrames = logger.FrameCount;

            if (logger.LastError is not null)
            {
                IsRecording = false;
                HasRecording = false;
                RecordingStatus = "ERROR";
                return;
            }
        }

        IsRecording = false;
        HasRecording = RecordedFrames > 0;
        RecordingStatus = HasRecording ? "SAVED" : "READY";
    }

    public void RefreshRecordingCounter()
    {
        if (!IsRecording || _multimeter is null)
            return;

        var logger = _multimeter.Logger;

        RecordedFrames = logger.FrameCount;

        if (logger.IsRecording)
            return;

        IsRecording = false;
        HasRecording = false;
        RecordingStatus = logger.LastError is not null
            ? "ERROR"
            : "READY";
    }



    private void UpdateReading(MultimeterReading reading)
    {
        if (reading.IsAcDc)
        {
            // Przy wejściu do AC+DC usuwamy stare wskazania.
            if (!IsAcDcMode)
            {
                AcDisplayValue = "----";
                DcDisplayValue = "----";
            }

            IsAcDcMode = true;

            if (reading.IsAcComponent == true)
                AcDisplayValue = reading.DisplayValue;
            else if (reading.IsAcComponent == false)
                DcDisplayValue = reading.DisplayValue;

            AcDcUnit = reading.Unit
                .Replace("AC+DC", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (string.IsNullOrEmpty(AcDcUnit))
                AcDcUnit = "V";
        }
        else
        {
            IsAcDcMode = false;
        }

        DisplayValue = reading.DisplayValue;
        Unit = reading.Unit;
        Function = reading.Function;
        Range = reading.IsAutoRange ? "AUTO" : "MANUAL";

        IsHold = reading.IsHold;
        IsMin = reading.IsMin;
        IsMax = reading.IsMax;
    }

    private void ClearReading()
    {
        IsAcDcMode = false;
        AcDisplayValue = "----";
        DcDisplayValue = "----";
        DisplayValue = "----";
        Unit = "";
        Range = "";

        IsHold = false;
        IsMin = false;
        IsMax = false;
    }

    private void Disconnect()
    {
        StopRecording();

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


        if (propertyName == nameof(Range))
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(IsAutoRange)));

            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(IsManualRange)));
        }

        if (propertyName == nameof(Function))
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(IsDC)));

            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(IsAC)));

            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(MeasurementType)));
        }

        if (propertyName == nameof(Unit))
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(OverlayUnit)));
        }

    }
}
