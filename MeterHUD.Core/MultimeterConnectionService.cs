
namespace MeterHUD.Core;



public sealed class MultimeterConnectionService
    : IMultimeterConnectionService
{
    private readonly IMultimeterFactory _factory;
    private readonly ConnectionRetryPolicy _retryPolicy;
    private readonly TimeSpan _readDelay;
    private readonly TimeSpan _reconnectDelay;
    
    private volatile IMultimeter? _device;
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private bool _disposed;
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public IMultimeter? CurrentDevice => _device;

    public event EventHandler<MultimeterReading>? ReadingReceived;
    public event EventHandler<bool>? ConnectionChanged;
    public event EventHandler? ReadingMissing;
    public event EventHandler<RecordingStoppedEventArgs>? RecordingStopped;

    public MultimeterConnectionService(
        IMultimeterFactory factory,
        int maxConsecutiveFailures = 3,
        TimeSpan? readDelay = null,
        TimeSpan? reconnectDelay = null)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));

        _retryPolicy = new ConnectionRetryPolicy(
            maxConsecutiveFailures);

        _readDelay = readDelay ?? TimeSpan.FromMilliseconds(200);
        _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(2);

        if (_readDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(readDelay));

        if (_reconnectDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(reconnectDelay));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_runTask is not null)
            return Task.CompletedTask;

        cancellationToken.ThrowIfCancellationRequested();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        _runTask = RunAsync(_cts.Token);

        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _device = _factory.Create();
                    _retryPolicy.Reset();

                    while (!token.IsCancellationRequested)
                    {
                        MultimeterReading? reading;

                        try
                        {
                            reading = await _device.ReadAsync(token);
                        }
                        catch (OperationCanceledException)
                            when (token.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (TimeoutException)
                        {
                            reading = null;
                        }

                        if (token.IsCancellationRequested)
                            break;

                        if (reading is not null)
                        {
                            _retryPolicy.ReportSuccess();

                            SetConnected(true);
                            ReadingReceived?.Invoke(this, reading);
                        }
                        else
                        {
                            _retryPolicy.ReportFailure();

                            if (_retryPolicy.ShouldReconnect)
                                break;

                            ReadingMissing?.Invoke(this, EventArgs.Empty);
                        }

                        await Task.Delay(_readDelay, token);
                    }
                }
                catch (OperationCanceledException)
                    when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // Błąd otwierania lub komunikacji HID.
                    // Ponowimy próbę po reconnectDelay.
                }
                finally
                {
                    SetConnected(false);
                    DisconnectDevice();
                }

                if (!token.IsCancellationRequested)
                    await Task.Delay(_reconnectDelay, token);
            }
        }
        catch (OperationCanceledException)
            when (token.IsCancellationRequested)
        {
            // Normalne zatrzymanie.
        }
        finally
        {
            SetConnected(false);
            DisconnectDevice();
        }
    }

    private void SetConnected(bool connected)
    {
        if (_isConnected == connected)
            return;

        _isConnected = connected;
        ConnectionChanged?.Invoke(this, connected);
    }


    private void DisconnectDevice()
    {
        var device = _device;
        _device = null;

        if (device is null)
            return;

        try
        {
            if (device is IRecordingDevice recordingDevice)
            {
                try
                {
                    recordingDevice.StopRecording();
                    RecordingStoppedEventArgs result = recordingDevice.GetRecordingResult();
                    RecordingStopped?.Invoke(this, result);
                }
                catch (IOException)
                {
                    // Błąd zamykania pliku CSV.
                }
                catch (ObjectDisposedException)
                {
                    // Logger został już zwolniony.
                }
            }
        }
        finally
        {
            try
            {
                device.Dispose();
            }
            catch (IOException)
            {
                // Błąd podczas zamykania HID.
            }
            catch (ObjectDisposedException)
            {
                // Urządzenie zostało już zwolnione.
            }
        }
    }
    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();

        try
        {
            if (_runTask is not null)
                await _runTask;
        }
        finally
        {
            _runTask = null;
            _cts.Dispose();
            _cts = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_runTask is not null)
        {
            throw new InvalidOperationException(
                "Przed Dispose należy wywołać StopAsync.");
        }

        _disposed = true;

        _cts?.Dispose();
        DisconnectDevice();

        GC.SuppressFinalize(this);
    }
}
