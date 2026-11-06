using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MultimeterOverlay.Services;
using MultimeterOverlay.Core;
using MultimeterOverlay.Models;
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
            if (_connectionState == value)
                return;

            _connectionState = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(ConnectionState)));

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    public string Status => ConnectionState switch
    {
        ConnectionState.Connected =>
            LocalizationService.Instance["StatusConnected"],

        ConnectionState.Waiting =>
            LocalizationService.Instance["StatusWaiting"],

        _ =>
            LocalizationService.Instance["StatusDisconnected"]
    };


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
        if (e.PropertyName is "Item[]" or nameof(LocalizationService.Language))
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Status)));
        }
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

        DeviceName = "UNI-T UT161E [TEST]";


        DisplayValue = "-8888.8";
        Unit = "mV";
        Function = "Napięcie AC+DC";
        Range = "AUTO";

        IsAcDcMode = false;
        AcDisplayValue = "888.88";
        DcDisplayValue = "-888.88";
        AcDcUnit = "mV";


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
            DeviceName = "UNI-T UT161E [SYMULACJA]";
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
        DeviceName = "UNI-T UT161E [SYMULACJA]";

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

        DeviceName = "UNI-T UT161E";
        Function = "Oczekiwanie na pomiar";
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
        IsDisplayTestMode || Range == "AUTO";

    public bool IsManualRange =>
        IsDisplayTestMode || Range == "MANUAL";

    public bool IsDC =>
        IsDisplayTestMode ||
        Function.Contains("DC", StringComparison.OrdinalIgnoreCase) ||
        Function.Contains("stały", StringComparison.OrdinalIgnoreCase);

    public bool IsAC =>
        IsDisplayTestMode ||
        Function.Contains("AC", StringComparison.OrdinalIgnoreCase) ||
        Function.Contains("przemienny", StringComparison.OrdinalIgnoreCase);


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

        if (_consecutiveTimeouts >= MaxConsecutiveTimeouts)
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
        ConnectionState = ConnectionState.Disconnected;
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
