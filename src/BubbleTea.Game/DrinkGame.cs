using AppFactory.Physics;

namespace BubbleTea.Game;

public enum CupState { Ready, Filling, Full, Drinking, Empty }

public sealed record DrinkSettings(
    double StartDegrees = 40,
    double StopDegrees = 30,
    double HoldSeconds = 0.35,
    double DrainPerSecond = 0.22,
    double FillPerSecond = 1.4);

public sealed class DrinkGame
{
    private readonly DrinkSettings _settings;
    private readonly DampedSpring _surface = new();
    private readonly DampedSpring _depth = new();
    private double _held;

    public DrinkGame(DrinkSettings settings)
    {
        if (!(settings.StopDegrees >= 0 && settings.StartDegrees > settings.StopDegrees &&
              settings.StartDegrees <= 75 && settings.HoldSeconds > 0 && double.IsFinite(settings.HoldSeconds) &&
              settings.DrainPerSecond > 0 && double.IsFinite(settings.DrainPerSecond) &&
              settings.FillPerSecond > 0 && double.IsFinite(settings.FillPerSecond)))
            throw new ArgumentOutOfRangeException(nameof(settings));
        _settings = settings;
    }

    public CupState State { get; private set; }
    public double Level { get; private set; }
    public double SurfaceTilt => Math.Clamp(_surface.Position, -1, 1);
    public double SurfaceDepthTilt => Math.Clamp(_depth.Position, -1, 1);
    public double WavePhase { get; private set; }

    public void Pour()
    {
        if (State is CupState.Ready or CupState.Empty)
        {
            CancelTilt();
            _surface.Reset();
            _depth.Reset();
            State = CupState.Filling;
        }
    }

    public void CancelTilt()
    {
        _held = 0;
        if (State == CupState.Drinking)
            State = CupState.Full;
    }

    public void Step(double seconds, double pitchDegrees, double rollDegrees)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
            return;

        _surface.Step(double.IsFinite(rollDegrees) ? Math.Clamp(rollDegrees / 45, -1, 1) : 0, seconds);
        pitchDegrees = double.IsFinite(pitchDegrees) ? pitchDegrees : 0;
        _depth.Step(Math.Clamp(pitchDegrees / 60, -1, 1), seconds);
        WavePhase = (WavePhase + seconds % 10 * 3) % (2 * Math.PI);

        if (State == CupState.Filling)
        {
            Level = Math.Min(1, Level + seconds * _settings.FillPerSecond);
            if (Level >= 1)
                State = CupState.Full;
            return;
        }

        if (State == CupState.Full)
        {
            if (pitchDegrees < _settings.StartDegrees)
            {
                _held = 0;
                return;
            }

            var remainingHold = _settings.HoldSeconds - _held;
            _held += seconds;
            if (seconds < remainingHold)
                return;

            State = CupState.Drinking;
            seconds -= remainingHold;
        }

        if (State != CupState.Drinking)
            return;
        if (pitchDegrees < _settings.StopDegrees)
        {
            CancelTilt();
            return;
        }

        Level = Math.Max(0, Level - seconds * _settings.DrainPerSecond);
        if (Level <= 0)
            State = CupState.Empty;
    }
}
