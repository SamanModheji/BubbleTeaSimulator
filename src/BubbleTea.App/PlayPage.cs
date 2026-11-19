using System.Diagnostics;
using BubbleTea.Game;
using SkiaSharp.Views.Maui.Controls;

namespace BubbleTea.App;

internal sealed class PlayPage : ContentPage
{
    private readonly DrinkGame _game;
    private readonly MauiTiltSource _tilt;
    private readonly DrinkAudio _audio;
    private readonly CupRenderer _renderer = new();
    private readonly SKCanvasView _canvas = new() { InputTransparent = true };
    private readonly Label _hint = new() { FontSize = 17, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _pour = Action("＋  Eingiessen", "#6C40AF");
    private readonly Button _sip = Action("✦  Halten & trinken", "#266B65");
    private readonly Button _center = Action("◎", "#62456B");
    private readonly IDispatcherTimer _timer;
    private long _lastFrame;
    private bool _touchHeld;
    private bool _closed;

    public PlayPage(DrinkGame game, MauiTiltSource tilt, DrinkAudio audio)
    {
        (_game, _tilt, _audio) = (game, tilt, audio);
        BackgroundColor = Color.FromArgb("#FFF6E9");
        var ink = Color.FromArgb("#432754");
        _hint.TextColor = ink;
        var title = new Label
        {
            Text = "Bubble Tea",
            FontSize = 34,
            FontAttributes = FontAttributes.Bold,
            TextColor = ink,
            HorizontalTextAlignment = TextAlignment.Center
        };
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
        SemanticProperties.SetDescription(_center, "Neigung aufrecht neu kalibrieren");
        _center.WidthRequest = 64;

        var grid = new Grid { Padding = new Thickness(16, 8), RowSpacing = 6, ColumnSpacing = 8 };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.Add(title, 0, 0);
        grid.Add(_canvas, 0, 1);
        grid.Add(_hint, 0, 2);
        grid.Add(_pour, 0, 3);
        grid.Add(_sip, 0, 3);
        grid.Add(_center, 1, 3);
        foreach (var fullWidth in new View[] { title, _canvas, _hint, _pour })
            Grid.SetColumnSpan(fullWidth, 2);
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);
        Content = grid;

        _pour.Clicked += (_, _) => _game.Pour();
        _center.Clicked += (_, _) =>
        {
            _touchHeld = false;
            _game.CancelTilt();
            _tilt.Recalibrate();
            _audio.SetDrinking(false);
            RefreshControls();
        };
        _sip.Pressed += (_, _) => _touchHeld = true;
        _sip.Released += (_, _) => _touchHeld = false;
        _canvas.PaintSurface += (_, args) =>
        {
            if (!_closed)
                _renderer.Draw(args.Surface.Canvas, args.Info.Width, args.Info.Height, _game);
        };
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1.0 / 60);
        _timer.Tick += (_, _) => AdvanceFrame();
        RefreshControls();
    }

    public void ResumeGame()
    {
        if (_closed || _timer.IsRunning)
            return;
        _tilt.Start();
        _lastFrame = Stopwatch.GetTimestamp();
        _canvas.InvalidateSurface();
        _timer.Start();
    }

    public void PauseGame()
    {
        _timer.Stop();
        _tilt.Stop();
        _touchHeld = false;
        _game.CancelTilt();
        _audio.Release();
        RefreshControls();
    }

    public void CloseGame()
    {
        PauseGame();
        _closed = true;
        _renderer.Dispose();
    }

    private void AdvanceFrame()
    {
        var now = Stopwatch.GetTimestamp();
        var seconds = Math.Min(0.05, Stopwatch.GetElapsedTime(_lastFrame, now).TotalSeconds);
        _lastFrame = now;
        var tilt = _tilt.Read();
        var before = _game.State;
        if (_tilt.ChangedReference && !_touchHeld)
            _game.CancelTilt();
        _game.Step(seconds, _touchHeld ? 90 : tilt.PitchDegrees, _touchHeld ? 10 : tilt.RollDegrees);
        if (_game.State != before)
            RefreshControls();
        if ((_game.State == CupState.Drinking) != (before == CupState.Drinking))
            _audio.SetDrinking(_game.State == CupState.Drinking);
        if (_game.Level > 0 || _game.State != before)
            _canvas.InvalidateSurface();
    }

    private void RefreshControls()
    {
        var state = _game.State;
        _pour.IsVisible = state is CupState.Ready or CupState.Empty;
        _pour.Text = state == CupState.Empty ? "↻  Nochmal!" : "＋  Eingiessen";
        _sip.IsVisible = _center.IsVisible = state is CupState.Full or CupState.Drinking;
        if (!_sip.IsVisible)
            _touchHeld = false;
        _hint.Text = state switch
        {
            CupState.Ready => "Mach dir einen Tee!",
            CupState.Filling => "Gluck, gluck …",
            CupState.Full => "Kipp das Tablet zu dir.",
            CupState.Drinking => "Schlürf!",
            _ => "Alles leer!"
        };
    }

    private static Button Action(string text, string background) => new()
    {
        Text = text,
        TextColor = Colors.White,
        BackgroundColor = Color.FromArgb(background),
        CornerRadius = 23,
        FontAttributes = FontAttributes.Bold,
        FontSize = 17,
        MinimumHeightRequest = 60
    };
}
