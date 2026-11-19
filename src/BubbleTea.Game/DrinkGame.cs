using AppFactory.Physics;

namespace BubbleTea.Game;

public enum CupState { Ready, Filling, Full, Drinking, Empty }

public sealed record DrinkSettings(
    double StartDegrees = 40,
    double StopDegrees = 30,
    double HoldSeconds = 0.35,
    double DrainPerSecond = 0.3,
    double FillPerSecond = 1.4,
    double FullFlowDegrees = 65,
    double MinFlow = 0.45);

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
              settings.FillPerSecond > 0 && double.IsFinite(settings.FillPerSecond) &&
              settings.FullFlowDegrees > settings.StopDegrees && settings.FullFlowDegrees <= 90 &&
              settings.MinFlow > 0 && settings.MinFlow <= 1))
            throw new ArgumentOutOfRangeException(nameof(settings));
        _settings = settings;
    }

    public CupState State { get; private set; }
    public double Level { get; private set; }
    public PourStream Stream { get; } = new();

    /// <summary>Sideways surface tilt from -1 to 1. Positive values raise the left edge of the surface.</summary>
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
            Stream.Reset();
            State = CupState.Filling;
        }
    }

    public void CancelTilt()
    {
        _held = 0;
        Stream.Stop();
        if (State == CupState.Drinking)
            State = CupState.Full;
    }

    /// <summary>
    /// Outflow from 0 to 1 for a drinking pitch: <see cref="DrinkSettings.MinFlow"/> at the stop threshold,
    /// rising linearly to 1 at <see cref="DrinkSettings.FullFlowDegrees"/>.
    /// </summary>
    public double FlowAt(double pitchDegrees)
    {
        if (!(pitchDegrees >= _settings.StopDegrees))
            return 0;
        var t = Math.Min(1, (pitchDegrees - _settings.StopDegrees) / (_settings.FullFlowDegrees - _settings.StopDegrees));
        return _settings.MinFlow + (1 - _settings.MinFlow) * t;
    }

    public void Step(double seconds, double pitchDegrees, double rollDegrees)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
            return;

        _surface.Step(double.IsFinite(rollDegrees) ? Math.Clamp(rollDegrees / 45, -1, 1) : 0, seconds);
        pitchDegrees = double.IsFinite(pitchDegrees) ? pitchDegrees : 0;
        _depth.Step(Math.Clamp(pitchDegrees / 60, -1, 1), seconds);
        WavePhase = (WavePhase + seconds % 10 * 3) % (2 * Math.PI);
        // The stream is advanced with exactly the outflow that lowered the level in this step.
        Stream.Step(seconds, UpdateLevel(seconds, pitchDegrees), SurfaceTilt);
    }

    private double UpdateLevel(double seconds, double pitchDegrees)
    {
        if (State == CupState.Filling)
        {
            Level = Math.Min(1, Level + seconds * _settings.FillPerSecond);
            if (Level >= 1)
                State = CupState.Full;
            return 0;
        }

        if (State == CupState.Full)
        {
            if (pitchDegrees < _settings.StartDegrees)
            {
                _held = 0;
                return 0;
            }

            var remainingHold = _settings.HoldSeconds - _held;
            _held += seconds;
            if (seconds < remainingHold)
                return 0;

            State = CupState.Drinking;
            seconds -= remainingHold;
        }

        if (State != CupState.Drinking)
            return 0;
        if (pitchDegrees < _settings.StopDegrees)
        {
            CancelTilt();
            return 0;
        }

        var flow = FlowAt(pitchDegrees);
        var before = Level;
        Level = Math.Max(0, Level - seconds * _settings.DrainPerSecond * flow);
        if (Level <= 0)
            State = CupState.Empty;
        return Level < before ? flow : 0;
    }
}
