using System.Numerics;
using AppFactory.Sensors;

namespace BubbleTea.Tests;

public class TiltFilterTests
{
    [Theory]
    [InlineData(ScreenRotation.Rotation0, 0, 1)]
    [InlineData(ScreenRotation.Rotation90, -1, 0)]
    [InlineData(ScreenRotation.Rotation180, 0, -1)]
    [InlineData(ScreenRotation.Rotation270, 1, 0)]
    public void ForwardPitchIsScreenRelative(ScreenRotation rotation, int x, int y)
    {
        var filter = new TiltFilter();
        Assert.Equal(default, filter.Update(new Vector3(x, y, 0), rotation, 1));
        var tilted = filter.Update(new Vector3(x, y, 1) / MathF.Sqrt(2), rotation, 5);
        Assert.Equal(45, tilted.PitchDegrees, 4);
        Assert.Equal(0, tilted.RollDegrees, 4);
    }

    [Fact]
    public void FilterSmoothsMotionAndDiscardsAccelerationSpikes()
    {
        var filter = new TiltFilter();
        filter.Update(Vector3.UnitY, ScreenRotation.Rotation0, 1);
        var first = filter.Update(Vector3.UnitX, ScreenRotation.Rotation0, 0.016);
        Assert.InRange(first.RollDegrees, 0.1, 20);
        Assert.Equal(first, filter.Update(Vector3.UnitX * 5, ScreenRotation.Rotation0, 1));
        var later = filter.Update(Vector3.UnitX, ScreenRotation.Rotation0, 1);
        Assert.InRange(later.RollDegrees, 89, 90);
    }

    [Fact]
    public void CalibrationAcceptsAnAlreadyTiltedNeutralAndResetsOnRotation()
    {
        var filter = new TiltFilter();
        var neutral = Vector3.Normalize(new Vector3(0, 1, 1));
        Assert.Equal(default, filter.Update(neutral, ScreenRotation.Rotation0, 1));
        Assert.Equal(-45, filter.Update(Vector3.UnitY, ScreenRotation.Rotation0, 5).PitchDegrees, 4);
        filter.Recalibrate();
        Assert.Equal(default, filter.Update(Vector3.UnitY, ScreenRotation.Rotation0, 1));
        Assert.Equal(default, filter.Update(neutral, ScreenRotation.Rotation90, 1));
    }
}
