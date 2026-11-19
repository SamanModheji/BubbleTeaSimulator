using System.Numerics;

namespace AppFactory.Sensors;

public enum ScreenRotation { Rotation0, Rotation90, Rotation180, Rotation270 }

public readonly record struct Tilt(double PitchDegrees, double RollDegrees);

public sealed class TiltFilter(double timeConstantSeconds = 0.12)
{
    private readonly double _timeConstant = double.IsFinite(timeConstantSeconds) && timeConstantSeconds > 0
        ? timeConstantSeconds
        : throw new ArgumentOutOfRangeException(nameof(timeConstantSeconds));
    private Vector3? _gravity;
    private Tilt? _neutral;
    private ScreenRotation? _rotation;

    public Tilt Update(Vector3 acceleration, ScreenRotation rotation, double seconds)
    {
        if (_rotation != rotation)
        {
            Recalibrate();
            _rotation = rotation;
        }

        var magnitude = acceleration.Length();
        if (float.IsFinite(magnitude) && magnitude is >= 0.5f and <= 1.5f)
        {
            var screenGravity = (rotation switch
            {
                ScreenRotation.Rotation90 => new Vector3(acceleration.Y, -acceleration.X, acceleration.Z),
                ScreenRotation.Rotation180 => new Vector3(-acceleration.X, -acceleration.Y, acceleration.Z),
                ScreenRotation.Rotation270 => new Vector3(-acceleration.Y, acceleration.X, acceleration.Z),
                _ => acceleration
            }) / magnitude;

            var alpha = (float)(1 - Math.Exp(-Math.Max(0, seconds) / _timeConstant));
            _gravity = _gravity is { } previous ? Vector3.Lerp(previous, screenGravity, alpha) : screenGravity;
        }

        if (_gravity is not { } gravity)
            return default;

        const double degrees = 180 / Math.PI;
        var angle = new Tilt(
            Math.Atan2(gravity.Z, gravity.Y) * degrees,
            Math.Atan2(gravity.X, Math.Sqrt(gravity.Y * gravity.Y + gravity.Z * gravity.Z)) * degrees);
        _neutral ??= angle;
        return new Tilt(Math.IEEERemainder(angle.PitchDegrees - _neutral.Value.PitchDegrees, 360),
            angle.RollDegrees - _neutral.Value.RollDegrees);
    }

    public void Recalibrate() => (_gravity, _neutral) = (null, null);
}
