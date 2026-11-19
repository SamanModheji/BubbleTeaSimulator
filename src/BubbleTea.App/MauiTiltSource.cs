using System.Diagnostics;
using System.Numerics;
using AppFactory.Sensors;

namespace BubbleTea.App;

internal sealed class MauiTiltSource
{
    private readonly TiltFilter _filter = new();
    private readonly IAccelerometer _hardware = Accelerometer.Default;
    private readonly object _sampleLock = new();
    private Vector3 _sample;
    private long _sampleTime;
    private long _processedTime;
    private Tilt _tilt;
    private volatile bool _running;
    private ScreenRotation? _previousRotation;

    public bool ChangedReference { get; private set; }

    public void Start()
    {
        if (_running || !_hardware.IsSupported)
            return;

        Recalibrate();
        _hardware.ReadingChanged += OnReading;
        try
        {
            _running = true;
            _hardware.Start(SensorSpeed.Game);
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or InvalidOperationException or Java.Lang.SecurityException)
        {
            _running = false;
            _hardware.ReadingChanged -= OnReading;
            Debug.WriteLine(ex);
        }
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _hardware.ReadingChanged -= OnReading;
        _hardware.Stop();
    }

    public void Recalibrate()
    {
        lock (_sampleLock)
            _sampleTime = 0;
        _processedTime = 0;
        _previousRotation = null;
        _filter.Recalibrate();
        _tilt = default;
    }

    public Tilt Read()
    {
        ChangedReference = false;
        Vector3 sample;
        long timestamp;
        lock (_sampleLock)
            (sample, timestamp) = (_sample, _sampleTime);

        if (!_running || timestamp == 0 || Stopwatch.GetElapsedTime(timestamp).TotalSeconds > 0.25)
        {
            ChangedReference = _processedTime != 0;
            if (ChangedReference)
                Recalibrate();
            return default;
        }

        if (timestamp == _processedTime)
            return _tilt;

        var rotation = DeviceDisplay.Current.MainDisplayInfo.Rotation switch
        {
            DisplayRotation.Rotation90 => ScreenRotation.Rotation90,
            DisplayRotation.Rotation180 => ScreenRotation.Rotation180,
            DisplayRotation.Rotation270 => ScreenRotation.Rotation270,
            _ => ScreenRotation.Rotation0
        };
        ChangedReference = _previousRotation != rotation;
        var seconds = _processedTime == 0 ? 0 : Stopwatch.GetElapsedTime(_processedTime, timestamp).TotalSeconds;
        _previousRotation = rotation;
        _processedTime = timestamp;
        return _tilt = _filter.Update(sample, rotation, seconds);
    }

    private void OnReading(object? sender, AccelerometerChangedEventArgs args)
    {
        var acceleration = args.Reading.Acceleration;
        if (!TiltFilter.IsUsableReading(acceleration))
            return;

        lock (_sampleLock)
        {
            if (!_running)
                return;
            _sample = acceleration;
            _sampleTime = Stopwatch.GetTimestamp();
        }
    }
}
