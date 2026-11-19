namespace AppFactory.Physics;

public sealed class DampedSpring
{
    private double _velocity;
    public double Position { get; private set; }

    public void Step(double target, double seconds, double frequency = 9)
    {
        // Closed-form critically damped response: stable even when frame times vary.
        var offset = Position - target;
        var impulse = _velocity + frequency * offset;
        var decay = Math.Exp(-frequency * seconds);
        Position = target + (offset + impulse * seconds) * decay;
        _velocity = (_velocity - frequency * impulse * seconds) * decay;
    }

    public void Reset() => (Position, _velocity) = (0, 0);
}
