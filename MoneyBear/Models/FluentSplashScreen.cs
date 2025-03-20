using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;
using MoneyBear.Views;

namespace MoneyBear.Models;

public class FluentSplashScreen : IApplicationSplashScreen
{
    public FluentSplashScreen()
    {
        SplashScreenContent = new SplashScreenView();
    }
    
    public async Task RunTasks(CancellationToken cancellationToken)
    {
        await ((SplashScreenView)SplashScreenContent).InitApp();
    }

    public string AppName { get; }
    public IImage AppIcon { get; }
    public object SplashScreenContent { get; }
    public int MinimumShowTime { get; }
}