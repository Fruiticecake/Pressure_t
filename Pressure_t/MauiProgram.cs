using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Maui.LifecycleEvents;
using Pressure_t.Services;
namespace Pressure_t;

public static class MauiProgram
{
    // 全局字体字段
    public static SKTypeface GlobalTypeface;

    public static MauiApp CreateMauiApp()
	{

		var builder = MauiApp.CreateBuilder();
		builder
            .UseSkiaSharp(true)
            .UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("msyh.ttc", "MicrosoftYaHei");
            });
        // 加载全局字体
        GlobalTypeface = SKTypeface.FromFile("Resources/Fonts/msyh.ttc");
        builder.ConfigureLifecycleEvents(lifecycle =>
        {
#if WINDOWS
                
                         lifecycle.AddWindows(windows => windows.OnWindowCreated((del) => {
                del.ExtendsContentIntoTitleBar = false;
            }));
#endif
        });

#if WINDOWS
            builder.Services.AddSingleton<ITrayService, Pressure_t.Platforms.Windows.TrayService>();
#endif
#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

