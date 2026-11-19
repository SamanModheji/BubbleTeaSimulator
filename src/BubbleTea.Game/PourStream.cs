namespace BubbleTea.Game;

/// <summary>
/// Render-independent state of the stream that leaves the cup while drinking.
/// Positions along the stream are fractions of the fall from the rim (0) to the table (1).
/// </summary>
public sealed class PourStream
{
    /// <summary>Seconds liquid needs to fall from the rim to the table.</summary>
    public const double FallSeconds = 0.45;

    /// <summary><see cref="DrinkGame.SurfaceTilt"/> beyond which a new stream picks the raised side.</summary>
    public const double SideThreshold = 0.1;

    /// <summary>True only while liquid is leaving the cup, i.e. exactly when the level sinks.</summary>
    public bool IsPouring { get; private set; }

    /// <summary>Current outflow from 0 to 1; zero whenever <see cref="IsPouring"/> is false.</summary>
    public double Flow { get; private set; }

    /// <summary>Stream width from 0 to 1. Keeps the last flow while a detached stream falls away.</summary>
    public double Thickness { get; private set; }

    /// <summary>How far the front of the stream has fallen.</summary>
    public double Head { get; private set; }

    /// <summary>Where the stream starts. Zero while attached to the rim; grows after pouring stops.</summary>
    public double Tail { get; private set; }

    /// <summary>+1 pours over the right rim, -1 over the left rim.</summary>
    public int Side { get; private set; } = 1;

    public bool IsVisible => Head > Tail;

    /// <summary>True once the front of the stream reaches the table.</summary>
    public bool HasLanded => IsVisible && Head >= 1;

    public double Phase { get; private set; }

    /// <summary>
    /// Normalised point on the falling stream: <c>Outward</c> is the horizontal distance from the rim
    /// (reaching 0.35–1 at the table, wider for stronger flow), <c>Drop</c> the vertical fall from 0 to 1.
    /// </summary>
    public (double Outward, double Drop) Trajectory(double position)
    {
        var u = double.IsFinite(position) ? Math.Clamp(position, 0, 1) : 0;
        return ((0.35 + 0.65 * Thickness) * u, 0.25 * u + 0.75 * u * u);
    }

    internal void Step(double seconds, double flow, double surfaceTilt)
    {
        var advance = seconds / FallSeconds;
        Phase = (Phase + seconds * 14) % (2 * Math.PI);
        if (flow > 0)
        {
            if (!IsVisible)
            {
                Head = 0;
                if (surfaceTilt > SideThreshold)
                    Side = -1;
                else if (surfaceTilt < -SideThreshold)
                    Side = 1;
            }

            IsPouring = true;
            Flow = Thickness = Math.Min(1, flow);
            Tail = 0;
            Head = Math.Min(1, Head + advance);
            return;
        }

        Stop();
        if (!IsVisible)
            return;
        Head = Math.Min(1, Head + advance);
        Tail = Math.Min(1, Tail + advance);
        if (Tail >= Head)
            (Head, Tail, Thickness) = (0, 0, 0);
    }

    internal void Stop() => (IsPouring, Flow) = (false, 0);

    internal void Reset()
    {
        Stop();
        (Head, Tail, Thickness, Phase) = (0, 0, 0, 0);
    }
}
