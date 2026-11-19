using BubbleTea.Game;

namespace BubbleTea.Tests;

public class DrinkGameTests
{
    private static readonly DrinkSettings Settings = new(45, 30, 0.5, 0.25, 1);

    [Fact]
    public void PourFillsCupAndClampsLevel()
    {
        var game = new DrinkGame(Settings);
        Assert.Equal(CupState.Ready, game.State);
        game.Step(1, 80, 0);
        Assert.Equal(0, game.Level);
        game.Pour();
        game.Step(0.25, 80, 0);
        Assert.Equal(CupState.Filling, game.State);
        Assert.Equal(0.25, game.Level);
        game.Step(2, 80, 0);
        Assert.Equal(CupState.Full, game.State);
        Assert.Equal(1, game.Level);
    }

    [Fact]
    public void DrinkingRequiresContinuousHoldInCorrectDirection()
    {
        var game = FullCup();
        game.Step(1, -60, 0);
        game.Step(0.375, 45, 0);
        game.Step(0.125, 44.9, 0);
        game.Step(0.375, 45, 0);
        Assert.Equal(CupState.Full, game.State);
        game.Step(0.125, 45, 0);
        Assert.Equal(CupState.Drinking, game.State);
        Assert.Equal(1, game.Level);
    }

    [Fact]
    public void DrinksAtConfiguredRateAndStopsBelowExitThreshold()
    {
        var game = FullCup();
        game.Step(0.75, 45, 0);
        Assert.Equal(0.9375, game.Level);
        game.Step(0.5, 30, 0);
        Assert.Equal(0.8125, game.Level);
        game.Step(1, 29.9, 0);
        Assert.Equal(CupState.Full, game.State);
        Assert.Equal(0.8125, game.Level);
        game.Pour();
        game.Step(0.25, 60, 0);
        game.CancelTilt();
        game.Step(0.25, 60, 0);
        Assert.Equal(CupState.Full, game.State);
        Assert.Equal(0.8125, game.Level);
    }

    [Fact]
    public void EmptyCupStaysEmptyUntilExplicitReplay()
    {
        var game = FullCup();
        game.Step(10, 70, 0);
        Assert.Equal(CupState.Empty, game.State);
        Assert.Equal(0, game.Level);
        game.Step(10, 70, 0);
        Assert.Equal(CupState.Empty, game.State);
        game.Pour();
        game.Step(1, 0, 0);
        Assert.Equal(CupState.Full, game.State);
        Assert.Equal(1, game.Level);
    }

    [Fact]
    public void SurfaceRespondsSmoothlyAndSettles()
    {
        var game = FullCup();
        game.Step(0.1, 60, 60);
        Assert.InRange(game.SurfaceTilt, 0.1, 0.9);
        Assert.InRange(game.SurfaceDepthTilt, 0.1, 0.9);
        game.Step(2, 0, 0);
        Assert.InRange(game.SurfaceTilt, -0.001, 0.001);
        Assert.InRange(game.SurfaceDepthTilt, -0.001, 0.001);
    }

    [Fact]
    public void InvalidThresholdsAndTimeAreRejectedOrIgnored()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrinkGame(Settings with { StopDegrees = 50 }));
        var game = FullCup();
        game.Step(double.NaN, 70, 0);
        game.Step(-1, 70, 0);
        Assert.Equal(1, game.Level);
        Assert.Equal(CupState.Full, game.State);
    }

    private static DrinkGame FullCup()
    {
        var game = new DrinkGame(Settings);
        game.Pour();
        game.Step(1, 0, 0);
        return game;
    }
}
