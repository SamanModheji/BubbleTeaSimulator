using System.Numerics;
using AppFactory.Sensors;
using BubbleTea.Game;

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
        var tilted = filter.Update(new Vector3(x, y, -1) / MathF.Sqrt(2), rotation, 5);
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
        var spike = Vector3.UnitX * 5;
        Assert.False(TiltFilter.IsUsableReading(spike));
        Assert.True(TiltFilter.IsUsableReading(Vector3.UnitX));
        Assert.Equal(first, filter.Update(spike, ScreenRotation.Rotation0, 1));
        var later = filter.Update(Vector3.UnitX, ScreenRotation.Rotation0, 1);
        Assert.InRange(later.RollDegrees, 89, 90);
    }

    [Fact]
    public void CalibrationAcceptsAnAlreadyTiltedNeutralAndResetsOnRotation()
    {
        var filter = new TiltFilter();
        var neutral = Vector3.Normalize(new Vector3(0, 1, -1));
        Assert.Equal(default, filter.Update(neutral, ScreenRotation.Rotation0, 1));
        Assert.Equal(-45, filter.Update(Vector3.UnitY, ScreenRotation.Rotation0, 5).PitchDegrees, 4);
        filter.Recalibrate();
        Assert.Equal(default, filter.Update(Vector3.UnitY, ScreenRotation.Rotation0, 1));
        Assert.Equal(default, filter.Update(neutral, ScreenRotation.Rotation90, 1));
    }

    [Theory]
    [InlineData(-1, CupState.Drinking)]
    [InlineData(1, CupState.Full)]
    public void OnlyTippingTopEdgeTowardPlayerDrains(int z, CupState expected)
    {
        var filter = new TiltFilter();
        var game = new DrinkGame(new DrinkSettings());
        game.Pour();
        game.Step(1, 0, 0);
        filter.Update(Vector3.UnitY, ScreenRotation.Rotation0, 1);
        var acceleration = new Vector3(0, 0.5f, z * MathF.Sqrt(0.75f));
        for (var i = 0; i < 50; i++)
        {
            var tilt = filter.Update(acceleration, ScreenRotation.Rotation0, 0.02);
            game.Step(0.02, tilt.PitchDegrees, tilt.RollDegrees);
        }

        Assert.Equal(expected, game.State);
        Assert.Equal(expected == CupState.Drinking, game.Level < 1);
    }
}
