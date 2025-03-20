using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace MoneyBear;

sealed class Program
{
    public static IDataProtector Protector;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddDataProtection()
            .SetApplicationName(Environment.GetEnvironmentVariable("MBAppName"))
            .Services
            .BuildServiceProvider();
        
        var protectorProvider = serviceProvider.GetService<IDataProtectionProvider>();
        Protector = protectorProvider.CreateProtector(Environment.GetEnvironmentVariable("MBAppName"));
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    } 
    
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();
        
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }
        
}