using BubbleTea.Game;

namespace BubbleTea.Tests;

public class PourStreamTests
{
    private const double Frame = 1.0 / 64;
    private static readonly DrinkSettings Settings = new(45, 30, 0.5, 0.25, 1, FullFlowDegrees: 60, MinFlow: 0.5);

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(29.9, 0)]
    [InlineData(30, 0.5)]
    [InlineData(45, 0.75)]
    [InlineData(60, 1)]
    [InlineData(90, 1)]
    public void FlowFollowsTiltBetweenThresholds(double pitch, double expected) =>
        Assert.Equal(expected, new DrinkGame(Settings).FlowAt(pitch), 10);

    [Fact]
    public void StreamPoursExactlyWhileLevelSinks()
    {
        var game = new DrinkGame(Settings);
        game.Pour();
        double[] pitches = [0, 44, 50, 70, 35, 31, 29, 10, 46, 46];
        foreach (var pitch in pitches)
        {
            for (var i = 0; i < 40; i++)
            {
                var before = game.Level;
                game.Step(Frame, pitch, 0);
                var sinking = game.Level < before;
                Assert.Equal(sinking, game.Stream.IsPouring);
                Assert.Equal(sinking ? game.FlowAt(pitch) : 0, game.Stream.Flow);
            }
        }

        Assert.Equal(CupState.Drinking, game.State);
    }

    [Fact]
    public void NoStreamWhileFillingOrBeforeHoldCompletes()
    {
        var game = new DrinkGame(Settings);
        game.Pour();
        game.Step(0.5, 90, 0);
        Assert.Equal(CupState.Filling, game.State);
        Assert.False(game.Stream.IsVisible);
        game.Step(0.5, 0, 0);
        game.Step(0.49, 50, 0);
        Assert.Equal(CupState.Full, game.State);
        Assert.False(game.Stream.IsVisible);
        game.Step(0.02, 50, 0);
        Assert.True(game.Stream.IsPouring);
        Assert.True(game.Stream.IsVisible);
    }

    [Fact]
    public void TiltingBackStopsOutflowAndDetachedStreamFallsAway()
    {
        var game = DrinkingCup(70);
        Assert.True(game.Stream.HasLanded);
        game.Step(Frame, 20, 0);
        Assert.Equal(CupState.Full, game.State);
        Assert.False(game.Stream.IsPouring);
        Assert.Equal(0, game.Stream.Flow);
        Assert.True(game.Stream.IsVisible);
        Assert.Equal(1, game.Stream.Thickness);

        var level = game.Level;
        var tail = game.Stream.Tail;
        var elapsed = Frame;
        while (game.Stream.IsVisible)
        {
            game.Step(Frame, 20, 0);
            elapsed += Frame;
            if (!game.Stream.IsVisible)
                break;
            Assert.True(game.Stream.Tail > tail);
            tail = game.Stream.Tail;
        }

        Assert.InRange(elapsed, PourStream.FallSeconds - Frame, PourStream.FallSeconds + Frame);
        Assert.Equal(level, game.Level);
        Assert.Equal(0, game.Stream.Thickness);
    }

    [Fact]
    public void RecalibrationStopsAndRefillResetsStream()
    {
        var game = DrinkingCup(60);
        game.CancelTilt();
        Assert.False(game.Stream.IsPouring);
        Assert.True(game.Stream.IsVisible);
        game.Step(Frame, 60, 0);
        Assert.False(game.Stream.IsPouring);

        var empty = DrinkingCup(90);
        while (empty.State != CupState.Empty)
            empty.Step(Frame, 90, 0);
        Assert.True(empty.Stream.IsPouring);
        empty.Step(Frame, 90, 0);
        Assert.False(empty.Stream.IsPouring);
        Assert.True(empty.Stream.IsVisible);
        empty.Pour();
        Assert.False(empty.Stream.IsVisible);
        Assert.Equal(0, empty.Stream.Thickness);
    }

    [Fact]
    public void FrontReachesTableAfterFallTime()
    {
        var game = DrinkingCup(60, holdOnly: true);
        var elapsed = 0.0;
        while (!game.Stream.HasLanded)
        {
            game.Step(Frame, 60, 0);
            elapsed += Frame;
            Assert.Equal(0, game.Stream.Tail);
        }

        Assert.InRange(elapsed, PourStream.FallSeconds - Frame, PourStream.FallSeconds + Frame);
        Assert.Equal(1, game.Stream.Head);
    }

    [Fact]
    public void TrajectoryFallsToTableAndReachesFurtherWithMoreTilt()
    {
        var gentle = DrinkingCup(31).Stream;
        var steep = DrinkingCup(80).Stream;
        Assert.Equal((0, 0), gentle.Trajectory(0));
        Assert.Equal(1, gentle.Trajectory(1).Drop, 10);
        Assert.Equal(gentle.Trajectory(1), gentle.Trajectory(5));
        Assert.Equal(gentle.Trajectory(0), gentle.Trajectory(double.NaN));
        Assert.True(steep.Trajectory(1).Outward > gentle.Trajectory(1).Outward);
        Assert.True(steep.Thickness > gentle.Thickness);

        var previous = steep.Trajectory(0);
        for (var u = 0.1; u <= 1; u += 0.1)
        {
            var point = steep.Trajectory(u);
            Assert.True(point.Outward > previous.Outward && point.Drop > previous.Drop);
            previous = point;
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, -1)]
    [InlineData(-20, 1)]
    public void StreamLeavesOverRaisedRimAndKeepsSideWhileVisible(double roll, int side)
    {
        var game = new DrinkGame(Settings);
        game.Pour();
        game.Step(1, 0, roll);
        game.Step(1, 0, roll);
        game.Step(0.6, 60, roll);
        Assert.Equal(side, game.Stream.Side);
        Assert.Equal(side > 0, game.SurfaceTilt <= PourStream.SideThreshold);
        game.Step(0.2, 60, -roll);
        Assert.Equal(side, game.Stream.Side);
    }

    private static DrinkGame DrinkingCup(double pitch, bool holdOnly = false)
    {
        var game = new DrinkGame(Settings);
        game.Pour();
        game.Step(1, 0, 0);
        game.Step(0.5, pitch < 45 ? 45 : pitch, 0);
        if (!holdOnly)
            for (var i = 0; i < 40; i++)
                game.Step(Frame, pitch, 0);
        return game;
    }
}
