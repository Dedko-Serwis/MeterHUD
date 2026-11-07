using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MultimeterOverlay.Services;
using MultimeterOverlay.Core;
using MultimeterOverlay.Models;
using MultimeterOverlay.Configuration;
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
    private string _deviceName = AppDefaults.DeviceName;
    private string _displayValue = AppDefaults.EmptyDisplay;
    private string _unit = AppDefaults.EmptyUnit;
    private string _function =        LocalizationService.Instance["StatusWaitingForMeasurement"];
    private string _range = AppDefaults.EmptyRange;
    private bool _isHold;
    private int _recordedFrames;
    private RecordingState _recordingState = RecordingState.Ready;
    private bool _isRecording;
    private bool _hasRecording;
    private bool _isMin;
    private bool _isMax;
    private bool _isAcDcMode;

    private string _acDisplayValue = AppDefaults.EmptyDisplay;
    private string _dcDisplayValue = AppDefaults.EmptyDisplay;
    private string _acDcUnit = AppDefaults.AcDcUnit;

    private int _consecutiveTimeouts;
    private bool _hasReceivedReading;
    private byte? _functionCode;
    private MultimeterReading? _lastReading;


    private readonly MultimeterSimulator _simulator = new();

    private readonly DispatcherTimer _simulationTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };

    private bool _isSimulationMode;

    public bool IsSimulationMode
    {
        get => _isSimulationMode;
        private set => SetField(ref _isSimulationMode, value);
    }


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


    private ConnectionState _connectionState =
        ConnectionState.Disconnected;

    public ConnectionState ConnectionState
    {
        get => _connectionState;
        private set
        {
            if (_connectionState == value) return;
            _connectionState = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionState)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    public string Status => ConnectionState switch
    {
        ConnectionState.Connected => LocalizationService.Instance["StatusConnected"],
        ConnectionState.Waiting => LocalizationService.Instance["StatusWaitingForMeter"],
        _ => LocalizationService.Instance["StatusDisconnected"]
    };


    private string GetDeviceDisplayName()
    {
        string name = AppDefaults.DeviceName;

        if (IsDisplayTestMode)
            return $"{name} {LocalizationService.Instance["DeviceDisplayTestSuffix"]}";

        if (IsSimulationMode)
            return $"{name} {LocalizationService.Instance["DeviceSimulationSuffix"]}";

        return name;
    }


    public bool IsHold
    {
        get => _isHold || IsDisplayTestMode;
        private set => SetField(ref _isHold, value);
    }

    public int RecordedFrames
    {
        get => _recordedFrames;
        private set => SetField(ref _recordedFrames, value);
    }



    public RecordingState RecordingState
    {
        get => _recordingState;
        private set
        {
            if (_recordingState == value) return;
            SetField(ref _recordingState, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecordingStatus)));
        }
    }


    public string RecordingStatus => RecordingState switch
    {
        RecordingState.Recording => LocalizationService.Instance["StatusRecording"],
        RecordingState.Saved => LocalizationService.Instance["StatusSaved"],
        RecordingState.Error => LocalizationService.Instance["StatusError"],
        _ => LocalizationService.Instance["StatusReady"]
    };


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

    private readonly DispatcherTimer _displayTestTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };

    private bool _isDisplayTestMode;
    private bool _testStartedSimulation;
    private int _displayTestStep;


    public MainViewModel()
    {
        LocalizationService.Instance.PropertyChanged +=
            Localization_PropertyChanged;
    }


    private void Localization_PropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not ("Item[]" or nameof(LocalizationService.Language)))
            return;

        // Nazwa urządzenia
        DeviceName = GetDeviceDisplayName();

        // Nazwa funkcji pomiarowej
        if (IsDisplayTestMode)
        {
            Function = LocalizationService.Instance["MeasurementVoltageACDC"];
        }
        else if (_lastReading is not null)
        {
            Function = MeasurementLocalizationService.GetFunctionName(_lastReading);
        }
        else
        {
            Function = LocalizationService.Instance["StatusWaitingForMeasurement"];
        }

        // Odświeżenie pozostałych tłumaczeń
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeasurementType)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecordingStatus)));
    }



    public bool IsDisplayTestMode
    {
        get => _isDisplayTestMode;
        private set
        {
            if (_isDisplayTestMode == value)
                return;

            SetField(ref _isDisplayTestMode, value);

            // Odświeżamy kontrolki korzystające
            // z właściwości obliczanych.
            OnIndicatorsChanged();
        }
    }


    public async Task EnableDisplayTestAsync()
    {
        if (IsDisplayTestMode)
            return;

        _testStartedSimulation = !IsSimulationMode;

        if (_testStartedSimulation)
            await EnableSimulationAsync();

        _simulationTimer.Stop();

        IsDisplayTestMode = true;
        _displayTestStep = 0;

        DeviceName = GetDeviceDisplayName();


        DisplayValue = "-8888.8";
        Unit = "mV";
        Function = LocalizationService.Instance["MeasurementVoltageACDC"];
        Range = AppDefaults.AutoRange;

        IsAcDcMode = false;
        AcDisplayValue = "888.88";
        DcDisplayValue = "-888.88";
        AcDcUnit = "mV";

        _displayTestTimer.Tick -= DisplayTestTimer_Tick;
        _displayTestTimer.Tick += DisplayTestTimer_Tick;
        _displayTestTimer.Start();


        DisplayTestTimer_Tick(this, EventArgs.Empty);
    }

    public void DisableDisplayTest()
    {
        if (!IsDisplayTestMode)
            return;

        _displayTestTimer.Stop();
        _displayTestTimer.Tick -= DisplayTestTimer_Tick;

        IsDisplayTestMode = false;

        if (_testStartedSimulation)
        {
            DisableSimulation();
        }
        else
        {
            DeviceName = GetDeviceDisplayName();
            _simulationTimer.Start();
            SimulationTimer_Tick(this, EventArgs.Empty);
        }

        _testStartedSimulation = false;
    }

    private void DisplayTestTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsDisplayTestMode) return;

        // Kolejne fazy testu.
        int step = _displayTestStep++ % 6;

        IsAcDcMode = step >= 3;

        ConnectionState = (step % 3) switch
        {
            0 => ConnectionState.Disconnected,
            1 => ConnectionState.Connected,
            _ => ConnectionState.Waiting
        };
    }


    private void OnIndicatorsChanged()
    {
        foreach (string name in new[]
        {
        nameof(IsAutoRange),
        nameof(IsManualRange),
        nameof(IsAC),
        nameof(IsDC),
        nameof(IsMin),
        nameof(IsMax),
        nameof(IsHold)
    })
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }


    public async Task EnableSimulationAsync()
    {
        if (IsSimulationMode)
            return;

        // Najpierw zatrzymujemy rzeczywisty miernik.
        await StopAsync();

        StopRecording();

        _simulator.Reset();

        IsSimulationMode = true;
        DeviceName = GetDeviceDisplayName();

        _simulationTimer.Tick -= SimulationTimer_Tick;
        _simulationTimer.Tick += SimulationTimer_Tick;

        _simulationTimer.Start();

        // Pierwszy pomiar bez czekania dwóch sekund.
        SimulationTimer_Tick(this, EventArgs.Empty);
    }

        public void DisableSimulation(bool restartDevice = true)
    {
        if (!IsSimulationMode)
            return;

        _simulationTimer.Stop();
        _simulationTimer.Tick -= SimulationTimer_Tick;

        IsSimulationMode = false;

        ClearReading();

        DeviceName = GetDeviceDisplayName();
        ConnectionState = ConnectionState.Disconnected;

        if (restartDevice)
            Start();
    }


    private void SimulationTimer_Tick(
        object? sender, EventArgs e)
    {
        if (!IsSimulationMode)
            return;

        MultimeterReading reading = _simulator.Next();

        UpdateReading(reading);

        ConnectionState = ConnectionState.Connected;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_readingTask is not null)
            return;

        _cts = new CancellationTokenSource();
        _readingTask = RunAsync(_cts.Token);
    }


    public bool IsAutoRange =>
        IsDisplayTestMode || Range == AppDefaults.AutoRange;

    public bool IsManualRange =>
        IsDisplayTestMode || Range == AppDefaults.ManualRange;


    public bool IsAC =>
        IsDisplayTestMode ||
        _functionCode is
            0x00 or 0x01 or
            0x0D or 0x0F or 0x11 or
            0x18 or 0x19;

    public bool IsDC =>
        IsDisplayTestMode ||
        _functionCode is
            0x02 or 0x03 or
            0x0C or 0x0E or 0x10 or
            0x19;



    public bool IsMin
    {
        get => _isMin || IsDisplayTestMode;
        private set => SetField(ref _isMin, value);
    }

    public bool IsMax
    {
        get => _isMax || IsDisplayTestMode;
        private set => SetField(ref _isMax, value);
    }


    public string MeasurementType
    {
        get
        {
            string? key = _functionCode switch
            {
                0x00 or 0x01 or 0x02 or 0x03 or
                0x18 or 0x19 => "MeasurementVoltage",

                0x0C or 0x0D or 0x0E or 0x0F or
                0x10 or 0x11 => "MeasurementCurrent",

                0x06 => "MeasurementResistance",
                0x07 => "MeasurementContinuity",
                0x08 => "MeasurementDiode",
                0x09 => "MeasurementCapacitance",
                0x04 => "MeasurementFrequency",
                0x05 => "MeasurementDutyCycle",
                0x12 => "MeasurementTransistor",

                _ => null
            };

            return key is null
                ? Function
                : LocalizationService.Instance[key];
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
        ConnectionState = ConnectionState.Disconnected;

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
                        ConnectionState = ConnectionState.Disconnected;

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
                            ConnectionState = ConnectionState.Connected;
                        }

                        else
                        {
                            HandleMissingReading();

                            if (_consecutiveTimeouts >= AppDefaults.MaxConsecutiveTimeouts)
                            {
                                reconnect = true;
                                break;
                            }
                        }

                        RefreshRecordingCounter();
                        await Task.Delay(ReadDelayMs, token);

                    }
                    catch (TimeoutException)
                    {
                        HandleMissingReading();

                        if (_consecutiveTimeouts >= AppDefaults.MaxConsecutiveTimeouts)
                            reconnect = true;
                    }
                    catch (OperationCanceledException)
                        when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // Communication error or HID is disconnected.
                        StopRecording();
                        ClearReading();

                        _consecutiveTimeouts = 0;
                        _hasReceivedReading = false;

                        ConnectionState = ConnectionState.Disconnected;
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

        if (_consecutiveTimeouts >= AppDefaults.MaxConsecutiveTimeouts)
        {
            StopRecording();
            ClearReading();

            _hasReceivedReading = false;
            ConnectionState = ConnectionState.Disconnected;
        }
        else if (_hasReceivedReading)
        {
            ConnectionState = ConnectionState.Waiting;
        }
        else
        {
            ConnectionState = ConnectionState.Disconnected;
        }
    }



    public void ToggleRecording()
    {
        if (IsSimulationMode || IsDisplayTestMode) return;

        if (IsRecording)
        {
            StopRecording();
            return;
        }

        if (_multimeter is null || ConnectionState != ConnectionState.Connected) return;

        var logger = _multimeter.Logger;
        logger.Start();

        RecordedFrames = logger.FrameCount;
        IsRecording = logger.IsRecording;

        if (logger.LastError is not null)
        {
            RecordingState = RecordingState.Error;
            HasRecording = false;
            return;
        }

        HasRecording = false;
        RecordingState = IsRecording ? RecordingState.Recording : RecordingState.Ready;
    }


    public void StopRecording()
    {

        if (_multimeter is null)
        {
            IsRecording = false;

            if (RecordingState == RecordingState.Recording)
                RecordingState = RecordingState.Error;

            return;
        }


        var logger = _multimeter.Logger;

        if (!IsRecording && !logger.IsRecording)
            return;

        logger.Stop();

        RecordedFrames = logger.FrameCount;
        IsRecording = false;

        if (logger.LastError is not null)
        {
            HasRecording = false;
            RecordingState = RecordingState.Error;
            return;
        }

        HasRecording = RecordedFrames > 0;
        RecordingState = HasRecording
            ? RecordingState.Saved
            : RecordingState.Ready;
    }



    public void RefreshRecordingCounter()
    {
        if (_multimeter is null)
            return;

        var logger = _multimeter.Logger;

        if (!IsRecording && !logger.IsRecording)
            return;

        RecordedFrames = logger.FrameCount;

        if (logger.IsRecording)
            return;

        // Logger zakończył pracę, mimo że ViewModel
        // nadal uważał rejestrację za aktywną.
        IsRecording = false;

        if (logger.LastError is not null)
        {
            HasRecording = false;
            RecordingState = RecordingState.Error;
        }
        else
        {
            HasRecording = RecordedFrames > 0;
            RecordingState = HasRecording
                ? RecordingState.Saved
                : RecordingState.Ready;
        }
    }




    private void UpdateReading(MultimeterReading reading)
    {
        if (reading.IsAcDc)
        {
            // Przy wejściu do AC+DC usuwamy stare wskazania.
            if (!IsAcDcMode)
            {
                AcDisplayValue = AppDefaults.EmptyDisplay;
                DcDisplayValue = AppDefaults.EmptyDisplay;
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

        _lastReading = reading;
        _functionCode = reading.FunctionCode;
        Function = MeasurementLocalizationService.GetFunctionName(reading);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAC)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDC)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeasurementType)));
        Range = reading.IsAutoRange ? AppDefaults.AutoRange : AppDefaults.ManualRange;
        IsHold = reading.IsHold;
        IsMin = reading.IsMin;
        IsMax = reading.IsMax;
    }


    private void ClearReading()
    {
        // Usunięcie informacji o poprzednim pomiarze
        _lastReading = null;
        _functionCode = null;

        // Wyświetlacz
        IsAcDcMode = false;
        AcDisplayValue = AppDefaults.EmptyDisplay;
        DcDisplayValue = AppDefaults.EmptyDisplay;
        DisplayValue = AppDefaults.EmptyDisplay;

        Unit = AppDefaults.EmptyUnit;
        Range = AppDefaults.EmptyRange;
        AcDcUnit = AppDefaults.AcDcUnit;

        Function = LocalizationService.Instance["StatusWaitingForMeasurement"];

        // Wskaźniki
        IsHold = false;
        IsMin = false;
        IsMax = false;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAC)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDC)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeasurementType)));
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
        ConnectionState = ConnectionState.Disconnected;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_readingTask is not null)
        {
            throw new InvalidOperationException("Przed Dispose należy wywołać StopAsync.");
        }

        _disposed = true;
        _cts?.Dispose();
        Disconnect();
        _simulationTimer.Stop();
        _simulationTimer.Tick -= SimulationTimer_Tick;
        _displayTestTimer.Stop();
        _displayTestTimer.Tick -= DisplayTestTimer_Tick;
        LocalizationService.Instance.PropertyChanged -= Localization_PropertyChanged;
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
