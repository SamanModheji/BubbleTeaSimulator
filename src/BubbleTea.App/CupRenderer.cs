using BubbleTea.Game;
using SkiaSharp;

namespace BubbleTea.App;

internal sealed class CupRenderer : IDisposable
{
    private static readonly SKRect Rim = new(45, 80, 295, 120);
    private static readonly SKColor Ink = new(99, 69, 108);
    private readonly SKPaint _paint = new() { IsAntialias = true };
    private readonly SKPath _cup = MakeCup();
    private readonly SKPathBuilder _surface = new();
    private readonly SKShader _glass = HorizontalGradient(
        new SKColor(160, 212, 223, 148), new SKColor(255, 255, 255, 116), new SKColor(174, 197, 232, 150));
    private readonly SKShader _tea = HorizontalGradient(
        new SKColor(196, 106, 68), new SKColor(252, 183, 118), new SKColor(209, 119, 88));

    public void Draw(SKCanvas canvas, int width, int height, DrinkGame game)
    {
        canvas.Clear(new SKColor(255, 246, 233));
        if (width <= 0 || height <= 0)
            return;

        var scale = Math.Min(width / 340f, height / 430f);
        var save = canvas.Save();
        canvas.Translate((width - 340 * scale) / 2, (height - 430 * scale) / 2);
        canvas.Scale(scale);

        Fill(new SKColor(248, 225, 214));
        canvas.DrawOval(new SKRect(17, 43, 323, 399), _paint);
        Fill(new SKColor(220, 186, 187, 155));
        canvas.DrawOval(new SKRect(61, 366, 279, 402), _paint);

        Fill(SKColors.White);
        canvas.DrawOval(Rim, _paint);
        Fill(new SKColor(201, 188, 219));
        canvas.DrawOval(new SKRect(52, 86, 288, 115), _paint);
        _paint.StrokeCap = SKStrokeCap.Round;
        Fill(new SKColor(117, 170, 179));
        _paint.StrokeWidth = 19;
        canvas.DrawLine(189, 309, 226, 29, _paint);
        Fill(new SKColor(178, 240, 226));
        _paint.StrokeWidth = 7;
        canvas.DrawLine(184, 310, 222, 29, _paint);

        Fill(SKColors.White, _glass);
        canvas.DrawPath(_cup, _paint);
        if (game.Level > 0)
            DrawLiquid(canvas, game);
        Fill(new SKColor(255, 255, 255, 130));
        canvas.DrawRoundRect(new SKRect(69, 122, 80, 269), 6, 6, _paint);
        Fill(new SKColor(255, 255, 255, 75));
        canvas.DrawRoundRect(new SKRect(257, 124, 262, 244), 2, 2, _paint);

        Fill(Ink);
        _paint.Style = SKPaintStyle.Stroke;
        _paint.StrokeWidth = 4;
        canvas.DrawPath(_cup, _paint);
        _paint.Color = new SKColor(250, 249, 253);
        canvas.DrawArc(Rim, 2, 176, false, _paint);
        canvas.RestoreToCount(save);
    }

    private void DrawLiquid(SKCanvas canvas, DrinkGame game)
    {
        float level = (float)game.Level;
        float pitch = (float)game.SurfaceDepthTilt;
        float y = 383 - level * 261 + pitch * 10 * level;
        float tilt = (float)game.SurfaceTilt * Math.Min(47, 150 * level);
        float wave = (float)Math.Sin(game.WavePhase) * Math.Min(4, 25 * level);
        float depth = Math.Min(13 + pitch * 10, 55 * level);
        var save = canvas.Save();
        canvas.ClipPath(_cup, antialias: true);

        _surface.MoveTo(45, y - tilt);
        _surface.CubicTo(105, y - tilt * .55f + wave, 233, y + tilt * .55f - wave, 295, y + tilt);
        _surface.LineTo(295, 395);
        _surface.LineTo(45, 395);
        _surface.Close();
        using (var body = _surface.Detach())
        {
            Fill(SKColors.White, _tea);
            canvas.DrawPath(body, _paint);
            canvas.ClipPath(body, antialias: true);
        }

        _surface.MoveTo(45, y - tilt);
        _surface.CubicTo(105, y - tilt * .55f + wave, 233, y + tilt * .55f - wave, 295, y + tilt);
        _surface.CubicTo(232, y + tilt * .55f + depth, 104, y - tilt * .55f + depth, 45, y - tilt);
        _surface.Close();
        using var top = _surface.Detach();
        Fill(new SKColor(255, 219, 163));
        canvas.DrawPath(top, _paint);
        _paint.Style = SKPaintStyle.Stroke;
        _paint.StrokeWidth = 1.5f;
        _paint.Color = new SKColor(255, 240, 207);
        canvas.DrawPath(top, _paint);

        for (var i = 0; i < 7; i++)
        {
            float x = 113 + 18 * i;
            float bubbleY = 352 - (i * 13 % 34) + 2 * (float)Math.Sin(game.WavePhase + i);
            Fill(new SKColor(117, 65, 87, 220));
            canvas.DrawCircle(x, bubbleY, 8, _paint);
            Fill(new SKColor(255, 219, 192, 155));
            canvas.DrawCircle(x - 2, bubbleY - 3, 2.5f, _paint);
        }
        canvas.RestoreToCount(save);
    }

    private static SKPath MakeCup()
    {
        using var path = new SKPathBuilder();
        path.MoveTo(45, 100);
        path.CubicTo(108, 124, 232, 124, 295, 100);
        path.LineTo(262, 344);
        path.CubicTo(255, 397, 85, 397, 78, 344);
        path.Close();
        return path.Detach();
    }

    private void Fill(SKColor color, SKShader? shader = null)
    {
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = color;
        _paint.Shader = shader;
    }

    private static SKShader HorizontalGradient(params SKColor[] colors) =>
        SKShader.CreateLinearGradient(new SKPoint(48, 0), new SKPoint(292, 0), colors, SKShaderTileMode.Clamp);

    public void Dispose()
    {
        _paint.Dispose();
        _cup.Dispose();
        _surface.Dispose();
        _glass.Dispose();
        _tea.Dispose();
    }
}
