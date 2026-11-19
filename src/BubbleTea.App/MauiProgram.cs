using BubbleTea.Game;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace BubbleTea.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<BubbleTeaApplication>().UseSkiaSharp();
        builder.Services.AddSingleton(new DrinkSettings());
        builder.Services.AddTransient<DrinkGame>();
        builder.Services.AddTransient<MauiTiltSource>();
        builder.Services.AddTransient<DrinkAudio>();
        builder.Services.AddTransient<PlayPage>();
        return builder.Build();
    }
}

public sealed class BubbleTeaApplication(IServiceProvider services) : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = services.GetRequiredService<PlayPage>();
        var window = new Window(page);
        window.Activated += (_, _) => page.ResumeGame();
        window.Deactivated += (_, _) => page.PauseGame();
        window.Stopped += (_, _) => page.PauseGame();
        window.Destroying += (_, _) => page.CloseGame();
        return window;
    }
}
